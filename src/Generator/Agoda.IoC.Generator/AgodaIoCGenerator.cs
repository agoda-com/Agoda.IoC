using Agoda.IoC.Generator.Emit;
using Agoda.IoC.Generator.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Agoda.IoC.Generator;

[Generator(LanguageNames.CSharp)]
internal sealed partial class AgodaIoCGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {

        var mapperClassDeclarations = context.SyntaxProvider
             .CreateSyntaxProvider(
                static (s, ctx) => IsSyntaxTargetForGeneration(s, ctx),
                static (context, ctx) => GetSemanticTargetForGeneration(context, ctx))
            .Where(static (r) => r != null);

        var compilationAndMappers = context.CompilationProvider.Combine(mapperClassDeclarations.Collect());
        context.RegisterImplementationSourceOutput(compilationAndMappers, static (spc, source) => Execute(source.Left, source.Right, spc));

    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node, CancellationToken cancellationToken)
    {
        return node is ClassDeclarationSyntax
        {
            AttributeLists.Count: > 0,
        } candidateClass && !candidateClass.Modifiers.Any(SyntaxKind.StaticKeyword); // should not contain static keyword
    }

    private static ClassDeclarationSyntax GetSemanticTargetForGeneration(GeneratorSyntaxContext ctx, CancellationToken cancellationToken)
    {
        Debug.Assert(ctx.Node is ClassDeclarationSyntax);
        var classDeclaration = Unsafe.As<ClassDeclarationSyntax>(ctx.Node);

        foreach (var attributeListSyntax in classDeclaration.AttributeLists)
        {
            foreach (var attributeSyntax in attributeListSyntax.Attributes)
            {
                if (ctx.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                    continue;

                var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                var fullName = attributeContainingTypeSymbol.ToDisplayString();
                if (Constants.RegistrationTypes.ContainsKey(fullName))
                    return classDeclaration;
            }
        }
        return null;
    }

    private static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> registerClasses, SourceProductionContext ctx)
    {
        var assemblyNameForMethod = compilation.AssemblyName.Replace(".", string.Empty).Replace(" ", string.Empty).Trim();

        if (registerClasses.IsDefaultOrEmpty)
        {
            AddSourceInternal(ctx, string.Empty, assemblyNameForMethod, string.Empty);
            return;
        }

        var registrationNamedTypeSymbols = new List<INamedTypeSymbol>()
        {
            compilation.GetTypeByMetadataName(Constants.TRANSIENT_ATTRIBUTE_NAME),
            compilation.GetTypeByMetadataName(Constants.PER_REQUEST_ATTRIBUTE_NAME),
            compilation.GetTypeByMetadataName(Constants.SCOPED_ATTRIBUTE_NAME),
            compilation.GetTypeByMetadataName(Constants.SINGLETON_ATTRIBUTE_NAME)
        };

        if (registrationNamedTypeSymbols.Any(x => x is null))
        {
            AddSourceInternal(ctx, string.Empty, assemblyNameForMethod, string.Empty);
            return;
        }

        var registrationDescriptors = new List<RegistrationDescriptor>();
        foreach (var registrationClassSyntax in registerClasses.Distinct())
        {
            var registrationClassModel = compilation.GetSemanticModel(registrationClassSyntax.SyntaxTree);
            if (registrationClassModel.GetDeclaredSymbol(registrationClassSyntax) is not INamedTypeSymbol registrationClassSymbol) continue;
            if (!registrationClassSymbol.HasRegisterAttribute(registrationNamedTypeSymbols)) continue;

            registrationDescriptors.Add(new RegistrationDescriptor(registrationClassSymbol));
        }

        if (!registrationDescriptors.Any()) {
            AddSourceInternal(ctx, string.Empty, assemblyNameForMethod, string.Empty);
            return;
        }

        var namespaces = new HashSet<string>();
        var registrationCodes = new StringBuilder();
        var nsbuilder = new StringBuilder();

        var normalRegistrationContexts = new List<RegistrationContext>();
        var OfcollectionRegistrationContexts = new List<RegistrationContext>();


        foreach (var registrationDescriptor in registrationDescriptors)
        {
            registrationDescriptor.Build();

            registrationDescriptor.RegistrationContexts.ForEach(rg =>
            {
                if (rg.IsCollection) { OfcollectionRegistrationContexts.Add(rg); }
                else { normalRegistrationContexts.Add(rg); }
            });
            foreach (var ns in registrationDescriptor.NameSpaces) { namespaces.Add(ns); }
        }

        // order ofcollection
        if (OfcollectionRegistrationContexts.Any())
        {
            OfcollectionRegistrationContexts = OfcollectionRegistrationContexts
                .OrderBy(rg => rg.ForType)
                .ThenBy(rg => rg.Order)
                .ToList();
        }

        registrationCodes.Append(SourceEmitter.Build(normalRegistrationContexts));
        if (OfcollectionRegistrationContexts.Any())
        {
            registrationCodes.AppendLine($"\t\t\t// Of Collection code");
            registrationCodes.Append(SourceEmitter.Build(OfcollectionRegistrationContexts));
        }

        foreach (var ns in namespaces) nsbuilder.AppendLine($"using {ns};");

        var generatedCode = Constants.GENERATE_CLASS_SOURCE
            .Replace("{0}", nsbuilder.ToString())
            .Replace("{1}", assemblyNameForMethod)
            .Replace("{2}", registrationCodes.ToString());

        AddSourceInternal(ctx, nsbuilder.ToString(), assemblyNameForMethod, registrationCodes.ToString());
    }

    private static void AddSourceInternal(SourceProductionContext ctx,
        string usingNamespace,
        string extensionMethodNamespace,
        string registrationCodes)
    {
        var generatedCode = Constants.GENERATE_CLASS_SOURCE
           .Replace("{0}", usingNamespace)
           .Replace("{1}", extensionMethodNamespace)
           .Replace("{2}", registrationCodes);

        ctx.AddSource("Agoda.IoC.ServiceCollectionExtension.g.cs", SourceText.From(generatedCode, Encoding.UTF8));

    }


}
