using Microsoft.CodeAnalysis;

namespace Agoda.IoC.Generator.Analyzers;

internal static class ReferencedAssemblyScanner
{
    private const string AbstractionsAssemblyName = "Agoda.IoC.Generator.Abstractions";
    private const string ExternallyProvidedAttributeName = "Agoda.IoC.Generator.Abstractions.ExternallyProvidedAttribute";

    internal static ISet<string> GetExternallyProvidedServices(Compilation compilation)
    {
        var services = new HashSet<string>(StringComparer.Ordinal);
        foreach (var assembly in GetAssembliesToInspect(compilation, includeCurrentAssembly: true))
        {
            foreach (var attribute in assembly.GetAttributes())
            {
                if (attribute.AttributeClass == null ||
                    !attribute.AttributeClass.IsMetadataType(ExternallyProvidedAttributeName) ||
                    attribute.ConstructorArguments.Length != 1 ||
                    attribute.ConstructorArguments[0].Value is not ITypeSymbol serviceType)
                {
                    continue;
                }

                foreach (var key in serviceType.GetResolutionKeys())
                {
                    services.Add(key);
                }
            }
        }

        return services;
    }

    internal static IEnumerable<Registration> GetReferencedRegistrations(Compilation compilation)
    {
        foreach (var assembly in GetAssembliesToInspect(compilation, includeCurrentAssembly: false))
        {
            if (!ReferencesAbstractions(assembly))
            {
                continue;
            }

            foreach (var type in GetNamedTypes(assembly.GlobalNamespace))
            {
                foreach (var registration in RegistrationDiscovery.GetRegistrations(type, isFromCurrentCompilation: false))
                {
                    yield return registration;
                }
            }
        }
    }

    private static IEnumerable<IAssemblySymbol> GetAssembliesToInspect(Compilation compilation, bool includeCurrentAssembly)
    {
        if (includeCurrentAssembly)
        {
            yield return compilation.Assembly;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var assembly in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            if (seen.Add(assembly.Identity.GetDisplayName()))
            {
                yield return assembly;
            }
        }
    }

    private static bool ReferencesAbstractions(IAssemblySymbol assembly)
    {
        return assembly.Modules
            .SelectMany(module => module.ReferencedAssemblySymbols)
            .Any(referencedAssembly => referencedAssembly.Identity.Name == AbstractionsAssemblyName);
    }

    private static IEnumerable<INamedTypeSymbol> GetNamedTypes(INamespaceSymbol namespaceSymbol)
    {
        foreach (var type in namespaceSymbol.GetTypeMembers())
        {
            foreach (var nestedType in GetNamedTypes(type))
            {
                yield return nestedType;
            }
        }

        foreach (var childNamespace in namespaceSymbol.GetNamespaceMembers())
        {
            foreach (var type in GetNamedTypes(childNamespace))
            {
                yield return type;
            }
        }
    }

    private static IEnumerable<INamedTypeSymbol> GetNamedTypes(INamedTypeSymbol type)
    {
        yield return type;

        foreach (var nestedType in type.GetTypeMembers())
        {
            foreach (var childType in GetNamedTypes(nestedType))
            {
                yield return childType;
            }
        }
    }
}
