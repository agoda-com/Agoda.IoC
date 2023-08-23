using Agoda.IoC.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Agoda.IoC.NetCore
{
    public static class StartupExtension
    {
        public static IServiceCollection AutoWireAssembly(
            this IServiceCollection services,
            Assembly[] assemblies,
            bool isMockMode,
            Action<ContainerRegistrationOption> option = null)
        {
            IDictionary<Type, List<KeyTypePair>> keysForTypes = new Dictionary<Type, List<KeyTypePair>>();
            var rtn = services.AutoWireAssembly<RegisterPerRequestAttribute>(assemblies, ServiceLifetime.Scoped, isMockMode, option, keysForTypes)
                .AutoWireAssembly<RegisterSingletonAttribute>(assemblies, ServiceLifetime.Singleton, isMockMode, option, keysForTypes)
                .AutoWireAssembly<RegisterTransientAttribute>(assemblies, ServiceLifetime.Transient, isMockMode, option, keysForTypes);
            services.RegisterKeyedFactory(keysForTypes);
            return rtn;
        }

        public static IServiceCollection AutoWireAssembly<T>(
            this IServiceCollection services, Assembly[] assemblies,
            ServiceLifetime serviceLifetime,
            bool isMockMode,
             Action<ContainerRegistrationOption> option = null,
            IDictionary<Type, List<KeyTypePair>> keysForTypes = null)
            where T : ContainerRegistrationAttribute
        {
            var containerRegistrationOption = new ContainerRegistrationOption();
            option?.Invoke(containerRegistrationOption);

            var registrations = assemblies
                .SelectMany(assembly => AssemblyHelper.GetAllTypes(assembly))
                .Where(type => type != null && type.IsClass)
                .Select(type => new
                {
                    ToType = type,
                    Attributes = type.GetCustomAttributes<T>(false)
                })
                .Where(x => x.Attributes.Any())
                .SelectMany(x => x.Attributes.Select(
                    attr => new RegistrationContext(attr, x.ToType, isMockMode)
                ))
                .ToList();

            if (!Validate(registrations, containerRegistrationOption, out var errors))
            {
                throw new RegistrationFailedException("There are validations errors, please see RegistrationContextExceptions Property for details", errors);
            }

            foreach (var reg in registrations)
            {
                if (reg.Key != null)
                {
                    // keyed instances is recorded here for registration later
                    AddToKeyedRegistrationList(reg, keysForTypes, serviceLifetime);
                    continue;
                }
               
                var toType = isMockMode && reg.MockType != null
                            ? reg.MockType
                            : reg.ToType;

                var serviceDescriptor = CreateServiceDescriptor(reg, serviceLifetime, toType);
                if (reg?.ReplaceServices == true)
                {
                    services.Replace(serviceDescriptor);
                }               
                else
                {
                    services.Add(serviceDescriptor);
                }
            }
            return services;
        }

        /// <summary>
        /// Create ServiceDescriptor from RegistrationContext
        /// </summary>
        /// <param name="registrationContext"></param>
        /// <param name="serviceLifetime"></param>
        /// <param name="toType"></param>
        /// <returns></returns>
        private static ServiceDescriptor CreateServiceDescriptor(
            RegistrationContext registrationContext, ServiceLifetime serviceLifetime, Type toType = null)
        {
            if (registrationContext.FactoryType != null)
            {
                return new ServiceDescriptor(registrationContext.FromType, (x) =>
                 {
                     var factoryInstance = Activator.CreateInstance(registrationContext.FactoryType);
                     var buildMethod = factoryInstance.GetType().GetMethod("Build");
                     Debug.Assert(buildMethod != null, nameof(buildMethod) + " != null"); // type is checked by RegistrationInfo.Validate()

                     return buildMethod.Invoke(factoryInstance, new[] { new NetCoreComponentResolver(x) });

                 }, serviceLifetime);
            }
            _ = toType ?? throw new ArgumentNullException(nameof(toType));
            return new ServiceDescriptor(registrationContext.FromType, toType, serviceLifetime);
        }

        /// <summary>
        /// Ensure keys for each keyed registered type are unique
        /// </summary>
        /// <exception cref="RegistrationFailedException"></exception>
        private static void AddToKeyedRegistrationList(RegistrationContext reg, IDictionary<Type, List<KeyTypePair>> keysForTypes, ServiceLifetime serviceLifetime)
        {
            if (!keysForTypes.TryGetValue(reg.FromType, out var keys))
            {
                keysForTypes.Add(reg.FromType, new List<KeyTypePair> { new KeyTypePair(reg.Key, reg.ToType, serviceLifetime) });
            }
            else if (keys.Any(x => x.Key == reg.Key))
            {
                var msg = $"{reg.ToType.FullName}: {nameof(ContainerRegistrationAttribute.Key)} \"{reg.Key}\" has " +
                          $"already been registered for {reg.FromType.FullName}. Keys must be unique.";
                throw new RegistrationFailedException(msg);
            }
            else
            {
                keysForTypes[reg.FromType].Add(new KeyTypePair(reg.Key, reg.ToType, serviceLifetime));
            }
        }

        private static void RegisterKeyedFactory(this IServiceCollection services, IDictionary<Type, List<KeyTypePair>> keysForTypes)
        {
            foreach (var key in keysForTypes)
            {
                var keyedFactoryInterfaceType = typeof(IKeyedComponentFactory<>).MakeGenericType(key.Key);
                var keyedFactoryImplementationType = typeof(KeyedComponentFactory<>).MakeGenericType(key.Key);

                var regObject = typeof(NetCoreKeyedRegistrations<>).MakeGenericType(key.Key);
                var regObjectInstance = Activator.CreateInstance(regObject,
                    key.Value.ToDictionary(x => x.Key,
                        y => y.Type));
                services.AddSingleton(regObject, provider => regObjectInstance);
                services.AddSingleton(keyedFactoryInterfaceType, keyedFactoryImplementationType);
                services.AddSingleton(typeof(IKeyedComponentResolver<>).MakeGenericType(key.Key), typeof(NetCoreKeyedComponentResolver<>).MakeGenericType(key.Key));
                foreach (var implementation in key.Value)
                {
                    services.Add(new ServiceDescriptor(implementation.Type, implementation.Type,
                                    implementation.ServiceLifetime));
                }
            }
        }

        private static bool Validate(List<RegistrationContext> registrations, ContainerRegistrationOption containerRegistrationOption, out List<RegistrationContextException> errors)
        {
            var isValid = true;
            errors = new List<RegistrationContextException>();
            foreach (var registration in registrations.Where(registration => !registration.Validation.IsValid))
            {
                isValid = false;
                var exception = new RegistrationContextException(registration, registration.Validation.ErrorMessage);
                errors.Add(exception);
                containerRegistrationOption
                    .OnRegistrationContextException?
                    .Invoke(exception);
            }
            return isValid;
        }
    }

    public class NetCoreComponentResolver : IComponentResolver
    {
        private readonly IServiceProvider _serviceProvider;

        public NetCoreComponentResolver(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public T Resolve<T>()
        {
            return _serviceProvider.GetService<T>();

        }
    }
}