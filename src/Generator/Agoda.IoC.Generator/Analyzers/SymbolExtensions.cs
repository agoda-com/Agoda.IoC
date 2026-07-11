using Microsoft.CodeAnalysis;

namespace Agoda.IoC.Generator.Analyzers;

internal static class SymbolExtensions
{
    internal static bool IsAssignableTo(this INamedTypeSymbol implementationType, ITypeSymbol serviceType)
    {
        if (implementationType.IsSameTypeOrOriginalDefinition(serviceType))
        {
            return true;
        }

        if (implementationType.AllInterfaces.Any(type => type.IsSameTypeOrOriginalDefinition(serviceType)))
        {
            return true;
        }

        var baseType = implementationType.BaseType;
        while (baseType != null)
        {
            if (baseType.IsSameTypeOrOriginalDefinition(serviceType))
            {
                return true;
            }

            baseType = baseType.BaseType;
        }

        return false;
    }

    internal static bool IsSameTypeOrOriginalDefinition(this ITypeSymbol candidate, ITypeSymbol expected)
    {
        if (SymbolEqualityComparer.Default.Equals(candidate, expected))
        {
            return true;
        }

        return candidate is INamedTypeSymbol candidateNamed &&
               expected is INamedTypeSymbol expectedNamed &&
               SymbolEqualityComparer.Default.Equals(candidateNamed.OriginalDefinition, expectedNamed.OriginalDefinition);
    }

    internal static bool IsMetadataType(this INamedTypeSymbol type, string metadataName)
    {
        return type.OriginalDefinition.GetFullMetadataName() == metadataName;
    }

    internal static string GetFullMetadataName(this INamedTypeSymbol type)
    {
        var typeParts = new Stack<string>();
        var currentType = type;
        while (currentType != null)
        {
            typeParts.Push(currentType.MetadataName);
            currentType = currentType.ContainingType;
        }

        var typeName = string.Join(".", typeParts);
        if (type.ContainingNamespace == null || type.ContainingNamespace.IsGlobalNamespace)
        {
            return typeName;
        }

        return $"{type.ContainingNamespace.ToDisplayString()}.{typeName}";
    }

    internal static IEnumerable<string> GetResolutionKeys(this ITypeSymbol type)
    {
        yield return type.GetTypeKey();

        if (type is INamedTypeSymbol { IsGenericType: true } namedType)
        {
            yield return namedType.OriginalDefinition.GetTypeKey();
        }
    }

    internal static string GetTypeKey(this ITypeSymbol type)
    {
        return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    internal static string FormatType(this ITypeSymbol type)
    {
        return type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
    }

    internal static Location GetLocation(this IParameterSymbol parameter, Registration registration)
    {
        return parameter.Locations.FirstOrDefault(location => location.IsInSource) ?? registration.Location;
    }
}
