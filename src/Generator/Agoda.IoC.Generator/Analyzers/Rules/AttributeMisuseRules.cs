using Microsoft.CodeAnalysis;

namespace Agoda.IoC.Generator.Analyzers;

internal static class AttributeMisuseRules
{
    private const string ImplementationFactoryMetadataName = "Agoda.IoC.Generator.Abstractions.IImplementationFactory`1";

    internal static void AnalyzeLocalRegistration(Registration registration, Action<Diagnostic> reportDiagnostic)
    {
        ValidateForAssignability(registration, reportDiagnostic);
        ValidateConcreteExclusivity(registration, reportDiagnostic);
        ValidateOrderRequiresCollection(registration, reportDiagnostic);
        ValidateFactoryCollectionExclusivity(registration, reportDiagnostic);
        ValidateFactoryCompatibility(registration, reportDiagnostic);
    }

    internal static void AnalyzeDuplicateCollectionOrders(ServiceGraph graph, Action<Diagnostic> reportDiagnostic)
    {
        var currentRegistrationSet = new HashSet<Registration>(graph.Current);

        foreach (var duplicateGroup in graph.All
                     .Where(registration => registration.IsCollection && registration.HasOrder && registration.Order != 0)
                     .GroupBy(registration => $"{registration.ServiceType.GetTypeKey()}:{registration.Order}")
                     .Where(group => group.Count() > 1))
        {
            foreach (var registration in duplicateGroup.Where(currentRegistrationSet.Contains))
            {
                reportDiagnostic(Diagnostic.Create(
                    Diagnostics.AttributeMisuse,
                    registration.Location,
                    $"Collection registration for service '{registration.ServiceType.FormatType()}' has duplicate Order = {registration.Order}."));
            }
        }
    }

    private static void ValidateForAssignability(Registration registration, Action<Diagnostic> reportDiagnostic)
    {
        if (registration.ForType == null || registration.ImplementationType.IsAssignableTo(registration.ForType))
        {
            return;
        }

        reportDiagnostic(Diagnostic.Create(
            Diagnostics.AttributeMisuse,
            registration.Location,
            $"Type '{registration.ImplementationType.FormatType()}' explicitly registers For = typeof({registration.ForType.FormatType()}), but it does not implement this type."));
    }

    private static void ValidateConcreteExclusivity(Registration registration, Action<Diagnostic> reportDiagnostic)
    {
        if (!registration.IsConcrete || registration.ForType == null)
        {
            return;
        }

        reportDiagnostic(Diagnostic.Create(
            Diagnostics.AttributeMisuse,
            registration.Location,
            $"Type '{registration.ImplementationType.FormatType()}' cannot specify both Concrete = true and For = typeof({registration.ForType.FormatType()})."));
    }

    private static void ValidateOrderRequiresCollection(Registration registration, Action<Diagnostic> reportDiagnostic)
    {
        if (registration.IsCollection || !registration.HasOrder || registration.Order == 0)
        {
            return;
        }

        reportDiagnostic(Diagnostic.Create(
            Diagnostics.AttributeMisuse,
            registration.Location,
            $"Type '{registration.ImplementationType.FormatType()}' cannot specify Order unless OfCollection = true."));
    }

    private static void ValidateFactoryCollectionExclusivity(Registration registration, Action<Diagnostic> reportDiagnostic)
    {
        if (!registration.IsCollection || registration.FactoryType == null)
        {
            return;
        }

        reportDiagnostic(Diagnostic.Create(
            Diagnostics.AttributeMisuse,
            registration.Location,
            $"Type '{registration.ImplementationType.FormatType()}' cannot specify both Factory and OfCollection = true."));
    }

    private static void ValidateFactoryCompatibility(Registration registration, Action<Diagnostic> reportDiagnostic)
    {
        if (registration.FactoryType == null)
        {
            return;
        }

        var implementedFactoryTypes = GetImplementationFactoryServiceTypes(registration.FactoryType);
        if (implementedFactoryTypes.Any(type => type.IsSameTypeOrOriginalDefinition(registration.ServiceType)))
        {
            return;
        }

        reportDiagnostic(Diagnostic.Create(
            Diagnostics.InvalidFactory,
            registration.Location,
            registration.FactoryType.FormatType(),
            registration.ServiceType.FormatType()));
    }

    private static IReadOnlyList<ITypeSymbol> GetImplementationFactoryServiceTypes(ITypeSymbol factoryType)
    {
        if (factoryType is not INamedTypeSymbol namedFactoryType)
        {
            return Array.Empty<ITypeSymbol>();
        }

        return namedFactoryType.AllInterfaces
            .Where(type => type.IsMetadataType(ImplementationFactoryMetadataName))
            .Select(type => type.TypeArguments[0])
            .ToArray();
    }
}
