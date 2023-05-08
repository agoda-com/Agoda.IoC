using Microsoft.CodeAnalysis;

namespace Agoda.IoC.Generator.Helpers;

internal static class SymbolExtensions
{
    internal static bool HasRegisterAttribute(this ISymbol symbol, IList<INamedTypeSymbol> registrationAttributeTypeSymbols)
    {
        return symbol
                .GetAttributes()
                .Any(a => registrationAttributeTypeSymbols
                    .Any(namedAttribute => SymbolEqualityComparer.Default.Equals(a.AttributeClass, namedAttribute)));
    }

    internal static bool IsRegisterAttribute(this INamedTypeSymbol attributeSymbol, IList<INamedTypeSymbol> registrationAttributeTypeSymbols)
    {
        return registrationAttributeTypeSymbols
                .Any(namedAttribute => SymbolEqualityComparer.Default.Equals(attributeSymbol, namedAttribute));
    }

}
