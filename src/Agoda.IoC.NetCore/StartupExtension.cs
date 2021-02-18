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
        public static IServiceCollection AutoWireAssembly(
            this IServiceCollection services,
            Assembly[] assemblies,
            bool isMockMode,
            Action<ContainerRegistrationOption> option = null)
        {
            return services.AutoWireAssembly<RegisterPerRequestAttribute>(assemblies, ServiceLifetime.Scoped, isMockMode, option)
                .AutoWireAssembly<RegisterSingletonAttribute>(assemblies, ServiceLifetime.Singleton, isMockMode, option)
                .AutoWireAssembly<RegisterTransientAttribute>(assemblies, ServiceLifetime.Transient, isMockMode, option);
        }

        public static IServiceCollection AutoWireAssembly<T>(
            this IServiceCollection services, Assembly[] assemblies,
            ServiceLifetime serviceLifetime,
            bool isMockMode,
             Action<ContainerRegistrationOption> option = null)
            where T : ContainerRegistrationAttribute
        {

            var containerRegistrationOption = new ContainerRegistrationOption();
            option?.Invoke(containerRegistrationOption);


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

            if (!Validate(registrations, containerRegistrationOption))
            {
                throw new RegistrationFailedException("There are validations error!!!");
            }

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
                        return buildMethod.Invoke(factoryInstance, null);

                    }, serviceLifetime));
                }
                else
                {
                    services.Add(new ServiceDescriptor(reg.FromType, toType, serviceLifetime));
                }

            }
            return services;
        }

        private static bool Validate(List<RegistrationContext> registrations, ContainerRegistrationOption containerRegistrationOption)
        {
            bool isValid = true;
            registrations.ForEach(reg => {
                if (!reg.Validation.IsValid)
                {
                    isValid = false;
                    containerRegistrationOption
                        .OnRegistrationContextException?
                        .Invoke(new RegistrationContextException(reg, reg.Validation.ErrorMessage));
                }
            });
            return isValid;
        }
    }
}
