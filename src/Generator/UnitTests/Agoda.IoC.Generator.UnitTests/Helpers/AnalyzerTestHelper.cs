using Agoda.IoC.Generator.Abstractions;
using Agoda.IoC.Generator.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Agoda.IoC.Generator.UnitTests.Helpers;

public static class AnalyzerTestHelper
{
    public static async Task<IReadOnlyList<Diagnostic>> GetDiagnostics(
        string source,
        params MetadataReference[] additionalReferences)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(
            source,
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp10));

        var compilation = CSharpCompilation.Create(
            "Agoda.IoC.Generator.AnalyzerTests",
            new[] { syntaxTree },
            GetReferences().Concat(additionalReferences),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new IoCGraphAnalyzer());
        return await compilation.WithAnalyzers(analyzers).GetAnalyzerDiagnosticsAsync();
    }

    public static MetadataReference CreateReference(string source, string assemblyName)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(
            source,
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp10));

        var compilation = CSharpCompilation.Create(
            assemblyName,
            new[] { syntaxTree },
            GetReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        if (!result.Success)
        {
            var diagnostics = string.Join(Environment.NewLine, result.Diagnostics);
            throw new InvalidOperationException($"Reference compilation failed:{Environment.NewLine}{diagnostics}");
        }

        return MetadataReference.CreateFromImage(stream.ToArray());
    }

    private static IEnumerable<MetadataReference> GetReferences()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
            .Concat(new[]
            {
                MetadataReference.CreateFromFile(typeof(ContainerRegistration).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(IoCGraphAnalyzer).Assembly.Location)
            })
            .Distinct();
    }
}
