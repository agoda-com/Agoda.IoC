using System;
using System.Collections.Generic;
using System.Linq;

namespace Agoda.IoC.Core
{
    public class ContainerInterceptorWrapper
    {
        private readonly List<(GenerateProxy GenerateProxy, Predicate<RegistrationContext> ShouldApply)> _descriptors;

        public delegate object GenerateProxy(Type interfaceType, object instance);
     
        public ContainerInterceptorWrapper()
        {
            _descriptors = new List<(GenerateProxy, Predicate<RegistrationContext>)>();
        }

        /// <summary>
        /// Registers an interceptor that will be applied to registrations that satisfy the predicate.
        /// </summary>
        /// <remarks>
        /// Interceptors will be applied in the order in which they are registered.
        /// </remarks>
        public void RegisterInterceptor(GenerateProxy generateProxy, Predicate<RegistrationContext> shouldApply)
        {
            _descriptors.Add((generateProxy, shouldApply));
        }

        /// <summary>
        /// Determines if this registration has interceptors applied.
        /// </summary>
        public Predicate<RegistrationContext> HasInterceptors => reg => _descriptors.Any(d => d.ShouldApply(reg));

        /// <summary>
        /// Wraps the given instance in all applicable interceptors.
        /// </summary>
        public object WrapInterceptors(object instanceToWrap, RegistrationContext reg)
        {
            if (!reg.IsIntercepted)
            {
                return instanceToWrap;
            }
            
            return _descriptors
                .Where(desc => desc.ShouldApply(reg))
                .Select(desc => desc.GenerateProxy)
                .Aggregate(instanceToWrap, (wrapped, generateProxy) => generateProxy(reg.FromType, wrapped));
        }
    }
}
