using Microsoft.CodeAnalysis;

namespace Agoda.IoC.Generator.Analyzers;

internal static class MissingRegistrationRule
{
    internal static void Analyze(ServiceGraph graph, Action<Diagnostic> reportDiagnostic)
    {
        foreach (var registration in graph.GetDistinctCurrentImplementationRegistrations())
        {
            if (registration.FactoryType != null)
            {
                continue;
            }

            foreach (var parameter in graph.GetMissingParameters(registration))
            {
                reportDiagnostic(Diagnostic.Create(
                    Diagnostics.MissingRegistration,
                    parameter.GetLocation(registration),
                    parameter.Name,
                    registration.ImplementationType.FormatType(),
                    parameter.Type.FormatType()));
            }
        }
    }
}
