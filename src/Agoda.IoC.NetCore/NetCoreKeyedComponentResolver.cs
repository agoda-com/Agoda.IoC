using System;
using System.Collections.Generic;
using System.Text;
using Agoda.IoC.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Agoda.IoC.NetCore
{
    public class NetCoreKeyedRegistrations<T>
    {
        public NetCoreKeyedRegistrations(IDictionary<object, Type> registrations)
        {
            Registrations = registrations;
        }

        public IDictionary<object, Type> Registrations { get; }
    }
    public class NetCoreKeyedComponentResolver<T> : IKeyedComponentResolver<T>
    {
        private readonly IServiceProvider _container;
        private readonly IDictionary<object, Type> _registrations;

        public NetCoreKeyedComponentResolver(IServiceProvider container, NetCoreKeyedRegistrations<T> registrations)
        {
            _container = container;
            _registrations = registrations.Registrations;
        }

        public T Resolve(object key)
        {
            if (!_registrations.TryGetValue(key, out var implementationType))
                throw new ArgumentException($"Service name '{key}' is not registered");
            return (T)_container.GetService(implementationType);
        }

        public bool IsRegistered(object key)
        {
            return !_registrations.TryGetValue(key, out var implementationType);
        }
    }
}
