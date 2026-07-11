using Microsoft.CodeAnalysis;

namespace Agoda.IoC.Generator.Analyzers;

internal static class DuplicateRegistrationRule
{
    internal static void Analyze(ServiceGraph graph, Action<Diagnostic> reportDiagnostic)
    {
        var currentRegistrationSet = new HashSet<Registration>(graph.Current);

        foreach (var duplicateGroup in graph.All
                     .Where(registration => !registration.IsCollection && !registration.IsReplaceService)
                     .GroupBy(registration => registration.ServiceType.GetTypeKey())
                     .Where(group => group.Count() > 1))
        {
            var implementations = string.Join(", ", duplicateGroup
                .Select(registration => registration.ImplementationType.FormatType())
                .Distinct()
                .OrderBy(name => name));

            foreach (var registration in duplicateGroup.Where(currentRegistrationSet.Contains))
            {
                reportDiagnostic(Diagnostic.Create(
                    Diagnostics.DuplicateRegistration,
                    registration.Location,
                    registration.ServiceType.FormatType(),
                    implementations));
            }
        }
    }
}
