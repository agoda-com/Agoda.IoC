using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Agoda.IoC.Generator.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class IoCGraphAnalyzer : DiagnosticAnalyzer
{
    internal static DiagnosticDescriptor MissingRegistration => Diagnostics.MissingRegistration;

    internal static DiagnosticDescriptor CaptiveDependency => Diagnostics.CaptiveDependency;

    internal static DiagnosticDescriptor DuplicateRegistration => Diagnostics.DuplicateRegistration;

    internal static DiagnosticDescriptor InvalidFactory => Diagnostics.InvalidFactory;

    internal static DiagnosticDescriptor AttributeMisuse => Diagnostics.AttributeMisuse;

    internal static DiagnosticDescriptor CircularDependency => Diagnostics.CircularDependency;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = Diagnostics.All;

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(startContext =>
        {
            var registrations = new ConcurrentBag<Registration>();

            startContext.RegisterSymbolAction(symbolContext =>
            {
                var type = (INamedTypeSymbol)symbolContext.Symbol;
                foreach (var registration in RegistrationDiscovery.GetRegistrations(type, isFromCurrentCompilation: true))
                {
                    registrations.Add(registration);
                    AttributeMisuseRules.AnalyzeLocalRegistration(registration, symbolContext.ReportDiagnostic);
                }
            }, SymbolKind.NamedType);

            startContext.RegisterCompilationEndAction(endContext =>
            {
                var currentRegistrations = registrations.ToArray();
                if (currentRegistrations.Length == 0)
                {
                    return;
                }

                var allRegistrations = currentRegistrations
                    .Concat(ReferencedAssemblyScanner.GetReferencedRegistrations(endContext.Compilation))
                    .ToArray();

                if (allRegistrations.Length == 0)
                {
                    return;
                }

                var graph = new ServiceGraph(
                    currentRegistrations,
                    allRegistrations,
                    ReferencedAssemblyScanner.GetExternallyProvidedServices(endContext.Compilation));

                DuplicateRegistrationRule.Analyze(graph, endContext.ReportDiagnostic);
                AttributeMisuseRules.AnalyzeDuplicateCollectionOrders(graph, endContext.ReportDiagnostic);
                MissingRegistrationRule.Analyze(graph, endContext.ReportDiagnostic);
                CaptiveDependencyRule.Analyze(graph, endContext.ReportDiagnostic);
                CircularDependencyRule.Analyze(graph, endContext.ReportDiagnostic);
            });
        });
    }
}
