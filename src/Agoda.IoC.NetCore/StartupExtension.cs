using Agoda.IoC.Core;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Agoda.IoC.NetCore
{
    public static class StartupExtension
    {
        public static IServiceCollection AutoWireAssembly(this IServiceCollection services, Assembly[] assemblies,
             bool isMockMode)
        {
            return services.AutoWireAssembly<RegisterPerRequestAttribute>(assemblies, ServiceLifetime.Scoped,isMockMode)
                .AutoWireAssembly<RegisterSingletonAttribute>(assemblies, ServiceLifetime.Singleton, isMockMode)
                .AutoWireAssembly<RegisterTransientAttribute>(assemblies, ServiceLifetime.Transient, isMockMode);
        }

        public static IServiceCollection AutoWireAssembly<T>(this IServiceCollection services, Assembly[] assemblies, ServiceLifetime serviceLifetime, bool isMockMode) 
            where T : ContainerRegistrationAttribute
        {
            var registrations = assemblies
                .SelectMany(assembly => assembly.GetExportedTypes())
                .Where(type => type.IsClass)
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
            foreach (var reg in registrations)
            {
                var toType = isMockMode && reg.MockType != null
                    ? reg.MockType
                    : reg.ToType;
                // Set up supporting registrations.
                // Collections is not included here as its supported in netcore ootb
                // keyed instances not included as its not supported in netcore
                if (reg.FactoryType != null)
                {
                    services.Add(new ServiceDescriptor(reg.FromType, (x) =>
                    {
                        var factoryInstance = Activator.CreateInstance(reg.FactoryType);
                        var buildMethod = factoryInstance.GetType().GetMethod("Build");
                        Debug.Assert(buildMethod != null, nameof(buildMethod) + " != null"); // type is checked by RegistrationInfo.Validate()

                        return buildMethod.Invoke(factoryInstance, new[] { new NetCoreComponentResolver(x) });

                    }, serviceLifetime));
                }
                else
                {
                    services.Add(new ServiceDescriptor(reg.FromType, toType, serviceLifetime));
                }

            }
            return services;
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
