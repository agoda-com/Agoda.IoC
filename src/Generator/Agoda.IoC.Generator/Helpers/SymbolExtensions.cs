using Microsoft.CodeAnalysis;

namespace Agoda.IoC.Generator.Helpers;

internal static class SymbolExtensions
{
    internal static bool HasRegisterAttribute(this ISymbol symbol, IList<INamedTypeSymbol> attributeSymbols)
        => symbol
            .GetAttributes()
            .Any(a => attributeSymbols
                                .Any(namedAttribute => SymbolEqualityComparer.Default.Equals(a.AttributeClass, namedAttribute)));

    internal static bool IsRegisterAttribute(this INamedTypeSymbol attributeSymbol, IList<INamedTypeSymbol> attributeSymbols)
        => attributeSymbols.Any(namedAttribute => SymbolEqualityComparer.Default.Equals(attributeSymbol, namedAttribute));


}
