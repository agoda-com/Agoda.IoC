using Agoda.IoC.Generator;
using Microsoft.CodeAnalysis;

namespace Agoda.IoC.Generator.Analyzers;

internal static class RegistrationDiscovery
{
    internal static IEnumerable<Registration> GetRegistrations(INamedTypeSymbol implementationType, bool isFromCurrentCompilation)
    {
        foreach (var attribute in implementationType.GetAttributes())
        {
            if (attribute.AttributeClass == null ||
                !Constants.AttributeRegistrationTypes.TryGetValue(attribute.AttributeClass.ToDisplayString(), out var registrationType))
            {
                continue;
            }

            var registrationProperties = GetRegistrationProperties(attribute);
            var serviceType = GetServiceType(implementationType, registrationProperties);
            yield return new Registration(
                implementationType,
                serviceType,
                GetLifetime(registrationType),
                attribute,
                registrationProperties.ForType,
                registrationProperties.FactoryType,
                registrationProperties.IsConcrete,
                registrationProperties.IsCollection,
                registrationProperties.IsReplaceService,
                registrationProperties.HasOrder,
                registrationProperties.Order,
                isFromCurrentCompilation);
        }
    }

    private static RegistrationProperties GetRegistrationProperties(AttributeData attribute)
    {
        var properties = new RegistrationProperties();
        foreach (var namedArgument in attribute.NamedArguments)
        {
            switch (namedArgument.Key)
            {
                case "For" when namedArgument.Value.Value is ITypeSymbol forType:
                    properties.ForType = forType;
                    break;
                case "Factory" when namedArgument.Value.Value is ITypeSymbol factoryType:
                    properties.FactoryType = factoryType;
                    break;
                case "Concrete" when namedArgument.Value.Value is bool isConcrete:
                    properties.IsConcrete = isConcrete;
                    break;
                case "ReplaceService" when namedArgument.Value.Value is bool isReplaceService:
                    properties.IsReplaceService = isReplaceService;
                    break;
                case "OfCollection" when namedArgument.Value.Value is bool isCollection:
                    properties.IsCollection = isCollection;
                    break;
                case "Order" when namedArgument.Value.Value is int order:
                    properties.HasOrder = true;
                    properties.Order = order;
                    break;
            }
        }

        return properties;
    }

    private static ITypeSymbol GetServiceType(INamedTypeSymbol implementationType, RegistrationProperties properties)
    {
        if (properties.IsConcrete)
        {
            return implementationType;
        }

        if (properties.ForType != null)
        {
            return properties.ForType;
        }

        return implementationType.Interfaces.FirstOrDefault() ?? implementationType;
    }

    private static RegistrationLifetime GetLifetime(RegistrationType registrationType)
    {
        return registrationType switch
        {
            RegistrationType.Singleton => RegistrationLifetime.Singleton,
            RegistrationType.HostedService => RegistrationLifetime.Singleton,
            RegistrationType.Scoped => RegistrationLifetime.Scoped,
            _ => RegistrationLifetime.Transient
        };
    }
}
