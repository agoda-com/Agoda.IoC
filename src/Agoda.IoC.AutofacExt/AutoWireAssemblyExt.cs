using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Agoda.IoC.Core;
using Autofac;
using Autofac.Core;

namespace Agoda.IoC.AutofacExt
{
    public static class AutoWireAssemblyExt
    {
        public static ContainerBuilder AutoWireAssembly(this ContainerBuilder services, Assembly[] assemblies,
            bool isMockMode)
        {
            return services.AutoWireAssembly<RegisterPerRequestAttribute>(assemblies, isMockMode)
                .AutoWireAssembly<RegisterSingletonAttribute>(assemblies, isMockMode)
                .AutoWireAssembly<RegisterTransientAttribute>(assemblies, isMockMode); ;
        }

        public static ContainerBuilder AutoWireAssembly<T>(this ContainerBuilder services, Assembly[] assemblies,
             bool isMockMode)
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
                // Collections is not included here 
                // keyed instances not included 
                if (reg.FactoryType != null)
                {
                    switch (typeof(T).Name)
                    {
                        case "RegisterSingletonAttribute":
                            services.Register((p) =>
                            {
                                var factoryInstance = Activator.CreateInstance(reg.FactoryType);
                                var buildMethod = factoryInstance.GetType().GetMethod("Build");
                                Debug.Assert(buildMethod != null, nameof(buildMethod) + " != null"); // type is checked by RegistrationInfo.Validate()
                                return buildMethod.Invoke(factoryInstance, null);
                            }).As(reg.FromType)
                                .SingleInstance();
                            break;
                        case "RegisterPerRequestAttribute":
                            services.Register((p) =>
                            {
                                var factoryInstance = Activator.CreateInstance(reg.FactoryType);
                                var buildMethod = factoryInstance.GetType().GetMethod("Build");
                                Debug.Assert(buildMethod != null, nameof(buildMethod) + " != null"); // type is checked by RegistrationInfo.Validate()
                                return buildMethod.Invoke(factoryInstance, null);
                            }).As(reg.FromType)
                                .InstancePerLifetimeScope();
                            break;
                        case "RegisterTransientAttribute":
                            services.Register((p) =>
                            {
                                var factoryInstance = Activator.CreateInstance(reg.FactoryType);
                                var buildMethod = factoryInstance.GetType().GetMethod("Build");
                                Debug.Assert(buildMethod != null, nameof(buildMethod) + " != null"); // type is checked by RegistrationInfo.Validate()
                                return buildMethod.Invoke(factoryInstance, null);
                            }).As(reg.FromType)
                                .InstancePerDependency();
                            break;
                    }
                }
                else
                {
                    switch (typeof(T).Name)
                    {
                        case "RegisterSingletonAttribute":
                            if (toType.IsGenericTypeDefinition)
                            {
                                services.RegisterGeneric(toType).As(reg.FromType).SingleInstance();
                            }
                            else
                            {
                                services.RegisterType(toType).As(reg.FromType).SingleInstance();
                            }
                            break;
                        case "RegisterPerRequestAttribute":
                            services.RegisterType(toType).As(reg.FromType).InstancePerLifetimeScope();
                            break;
                        case "RegisterTransientAttribute":
                            services.RegisterType(toType).As(reg.FromType).InstancePerDependency();
                            break;
                    }
                }

            }
            return services;
        }

        public static IContainer UseStartupable(this IContainer container)
        {
            var listOfStartupableServiceRegistrations =
                container.ComponentRegistry.Registrations
                    .Where(x => typeof(IStartupable).IsAssignableFrom(x.Target.Activator.LimitType))
                    .SelectMany(y => y.Services);
                
            foreach (var StartupableService in listOfStartupableServiceRegistrations)
            {
                ((IStartupable)container.ResolveService(StartupableService)).Start();
            }

            return container;
        }


        private static bool ContainsType(IEnumerable<Autofac.Core.Service> services, Type t)
        {
            return services.Where(y => y is TypedService)
                .Any(z => ((TypedService)z).ServiceType == t);
        }

    }
}
