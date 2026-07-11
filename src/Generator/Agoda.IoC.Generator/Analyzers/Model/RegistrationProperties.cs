using Microsoft.CodeAnalysis;

namespace Agoda.IoC.Generator.Analyzers;

internal sealed class RegistrationProperties
{
    public ITypeSymbol ForType { get; set; }

    public ITypeSymbol FactoryType { get; set; }

    public bool IsConcrete { get; set; }

    public bool IsCollection { get; set; }

    public bool IsReplaceService { get; set; }

    public bool HasOrder { get; set; }

    public int Order { get; set; }
}
