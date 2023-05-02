using Agoda.IoC.Generator.Emit;
using Agoda.IoC.Generator.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
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

        var targetClassDeclarations = context.SyntaxProvider
             .CreateSyntaxProvider(
                static (s, ctx) => IsSyntaxTargetForGeneration(s, ctx),
                static (context, ctx) => GetSemanticTargetForGeneration(context, ctx))
            .Where(static (r) => r != null);

        var compilationAndTargetClasses = context.CompilationProvider.Combine(targetClassDeclarations.Collect());
        context.RegisterImplementationSourceOutput(compilationAndTargetClasses, static (spc, source) => Execute(source.Left, source.Right, spc));

    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node, CancellationToken cancellationToken)
    {
        return node is ClassDeclarationSyntax
        {
            AttributeLists.Count: > 0,
        } targetClass && !targetClass.Modifiers.Any(SyntaxKind.StaticKeyword); // should not contain static keyword
    }

    private static ClassDeclarationSyntax GetSemanticTargetForGeneration(GeneratorSyntaxContext ctx, CancellationToken cancellationToken)
    {
        Debug.Assert(ctx.Node is ClassDeclarationSyntax);
        var classDeclaration = Unsafe.As<ClassDeclarationSyntax>(ctx.Node);

        foreach (var attributeList in classDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                if (ctx.SemanticModel.GetSymbolInfo(attribute).Symbol is not IMethodSymbol attributeMethodSymbol)
                    continue;

                var attributeDeclaringTypeSymbol = attributeMethodSymbol.ContainingType;
                var attributeTypeName = attributeDeclaringTypeSymbol.ToDisplayString();
                if (Constants.AttributeRegistrationTypes.ContainsKey(attributeTypeName))
                    return classDeclaration;
            }
        }
        return null;
    }

    private static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> registerClasses, SourceProductionContext ctx)
    {
        var assemblyNameForMethod = compilation.AssemblyName.Replace(".", string.Empty).Replace(" ", string.Empty).Trim();

        if (registerClasses.IsDefaultOrEmpty) { return; }

        var registrationAttributeTypeSymbols = new List<INamedTypeSymbol>()
        {
            compilation.GetTypeByMetadataName(Constants.RegisterTransientAttributeName),
            compilation.GetTypeByMetadataName(Constants.RegisterPerRequestAttributeName),
            compilation.GetTypeByMetadataName(Constants.RegisterScopedAttributeName),
            compilation.GetTypeByMetadataName(Constants.RegisterSingletonAttributeName)
        };

        if (registrationAttributeTypeSymbols.Any(x => x is null)) { return; }

        var registrationDescriptors = new List<RegistrationDescriptor>();
        foreach (var targetClassDeclaration in registerClasses.Distinct())
        {
            var registrationClassModel = compilation.GetSemanticModel(targetClassDeclaration.SyntaxTree);
            if (registrationClassModel.GetDeclaredSymbol(targetClassDeclaration) is not INamedTypeSymbol registrationClassSymbol) continue;
            if (!registrationClassSymbol.HasRegisterAttribute(registrationAttributeTypeSymbols)) continue;

            registrationDescriptors.Add(new RegistrationDescriptor(registrationClassSymbol));
        }

        if (!registrationDescriptors.Any()) { return; }

        var usedNamespaces = new HashSet<string>();
        var registrationCodes = new StringBuilder();
        var namespaceStringBuilder = new StringBuilder();

        var singleRegistrationContexts = new List<RegistrationContext>();
        var collectionRegistrationContexts = new List<RegistrationContext>();


        foreach (var registrationDescriptor in registrationDescriptors)
        {
            registrationDescriptor.Build();

            registrationDescriptor.RegistrationContexts.ForEach(registrationContext =>
            {
                if (registrationContext.IsCollection) { collectionRegistrationContexts.Add(registrationContext); }
                else { singleRegistrationContexts.Add(registrationContext); }
            });
            foreach (var ns in registrationDescriptor.NameSpaces) { usedNamespaces.Add(ns); }
        }

        // order ofcollection
        if (collectionRegistrationContexts.Any())
        {
            collectionRegistrationContexts = collectionRegistrationContexts
                .OrderBy(registrationContext => registrationContext.ForType)
                .ThenBy(registrationContext => registrationContext.Order)
                .ToList();
        }

        registrationCodes.Append(SourceEmitter.Build(singleRegistrationContexts));
        if (collectionRegistrationContexts.Any())
        {
            registrationCodes.AppendLine($"\t\t\t// Of Collection code");
            registrationCodes.Append(SourceEmitter.Build(collectionRegistrationContexts));
        }

        foreach (var ns in usedNamespaces) 
        { 
            namespaceStringBuilder.AppendLine($"using {ns};"); 
        }

        var generatedCode = Constants.GENERATE_CLASS_SOURCE
                    .Replace("{0}", namespaceStringBuilder.ToString())
                    .Replace("{1}", assemblyNameForMethod)
                    .Replace("{2}", registrationCodes.ToString());

        ctx.AddSource("Agoda.IoC.ServiceCollectionExtension.g.cs", SourceText.From(generatedCode, Encoding.UTF8));
    }
}
