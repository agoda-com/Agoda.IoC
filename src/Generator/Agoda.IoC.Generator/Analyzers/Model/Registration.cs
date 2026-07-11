using Microsoft.CodeAnalysis;

namespace Agoda.IoC.Generator.Analyzers;

internal sealed class Registration
{
    public Registration(
        INamedTypeSymbol implementationType,
        ITypeSymbol serviceType,
        RegistrationLifetime lifetime,
        AttributeData attribute,
        ITypeSymbol forType,
        ITypeSymbol factoryType,
        bool isConcrete,
        bool isCollection,
        bool isReplaceService,
        bool hasOrder,
        int order,
        bool isFromCurrentCompilation)
    {
        ImplementationType = implementationType;
        ServiceType = serviceType;
        Lifetime = lifetime;
        Attribute = attribute;
        ForType = forType;
        FactoryType = factoryType;
        IsConcrete = isConcrete;
        IsCollection = isCollection;
        IsReplaceService = isReplaceService;
        HasOrder = hasOrder;
        Order = order;
        IsFromCurrentCompilation = isFromCurrentCompilation;
    }

    public INamedTypeSymbol ImplementationType { get; }

    public ITypeSymbol ServiceType { get; }

    public RegistrationLifetime Lifetime { get; }

    public AttributeData Attribute { get; }

    public ITypeSymbol ForType { get; }

    public ITypeSymbol FactoryType { get; }

    public bool IsConcrete { get; }

    public bool IsCollection { get; }

    public bool IsReplaceService { get; }

    public bool HasOrder { get; }

    public int Order { get; }

    public bool IsFromCurrentCompilation { get; }

    public Location Location =>
        Attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ??
        ImplementationType.Locations.FirstOrDefault(location => location.IsInSource) ??
        Location.None;
}
