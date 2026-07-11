using Microsoft.CodeAnalysis;

namespace Agoda.IoC.Generator.Analyzers;

internal static class CaptiveDependencyRule
{
    internal static void Analyze(ServiceGraph graph, Action<Diagnostic> reportDiagnostic)
    {
        foreach (var registration in graph.GetDistinctCurrentImplementationRegistrations())
        {
            if (registration.Lifetime != RegistrationLifetime.Singleton || registration.FactoryType != null)
            {
                continue;
            }

            foreach (var dependency in graph.GetConstructorDependencies(registration))
            {
                foreach (var dependencyRegistration in dependency.Registrations.Where(item => item.Lifetime == RegistrationLifetime.Scoped))
                {
                    reportDiagnostic(Diagnostic.Create(
                        Diagnostics.CaptiveDependency,
                        dependency.Parameter.GetLocation(registration),
                        registration.ImplementationType.FormatType(),
                        dependency.Parameter.Type.FormatType(),
                        dependencyRegistration.ImplementationType.FormatType()));
                }
            }
        }
    }
}
