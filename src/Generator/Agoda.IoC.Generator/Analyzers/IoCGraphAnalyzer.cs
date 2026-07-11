using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Agoda.IoC.Generator.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class IoCGraphAnalyzer : DiagnosticAnalyzer
{
    private const string ExternallyProvidedAttributeName = "Agoda.IoC.Generator.Abstractions.ExternallyProvidedAttribute";
    private const string ImplementationFactoryMetadataName = "Agoda.IoC.Generator.Abstractions.IImplementationFactory`1";
    private const string AbstractionsAssemblyName = "Agoda.IoC.Generator.Abstractions";

    internal static readonly DiagnosticDescriptor MissingRegistration = new(
        "AGIOC001",
        "Constructor dependency is not registered",
        "Constructor parameter '{0}' of registered type '{1}' has type '{2}', which is not registered and is not marked with ExternallyProvided",
        "Agoda.IoC",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor CaptiveDependency = new(
        "AGIOC002",
        "Singleton captures scoped dependency",
        "Captive dependency: singleton registration '{0}' depends on scoped service '{1}' implemented by '{2}'",
        "Agoda.IoC",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor DuplicateRegistration = new(
        "AGIOC003",
        "Service type has multiple non-collection registrations",
        "Service type '{0}' has multiple non-collection registrations without ReplaceService = true: {1}",
        "Agoda.IoC",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor InvalidFactory = new(
        "AGIOC004",
        "Factory type is incompatible with registration",
        "Factory type '{0}' must implement IImplementationFactory<T> for registered service '{1}'",
        "Agoda.IoC",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor AttributeMisuse = new(
        "AGIOC005",
        "Registration attribute is invalid",
        "{0}",
        "Agoda.IoC",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor CircularDependency = new(
        "AGIOC006",
        "Circular constructor dependency",
        "Circular constructor dependency detected: {0}",
        "Agoda.IoC",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            MissingRegistration,
            CaptiveDependency,
            DuplicateRegistration,
            InvalidFactory,
            AttributeMisuse,
            CircularDependency);

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
                foreach (var registration in GetRegistrations(type, isFromCurrentCompilation: true))
                {
                    registrations.Add(registration);
                    AnalyzeLocalRegistration(registration, symbolContext.ReportDiagnostic);
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
                    .Concat(GetReferencedRegistrations(endContext.Compilation))
                    .ToArray();

                if (allRegistrations.Length == 0)
                {
                    return;
                }

                var externallyProvidedServices = GetExternallyProvidedServices(endContext.Compilation);
                var serviceMap = BuildServiceMap(allRegistrations);

                AnalyzeDuplicateRegistrations(currentRegistrations, allRegistrations, endContext.ReportDiagnostic);
                AnalyzeDuplicateCollectionOrders(currentRegistrations, allRegistrations, endContext.ReportDiagnostic);
                AnalyzeMissingRegistrations(currentRegistrations, serviceMap, externallyProvidedServices, endContext.ReportDiagnostic);
                AnalyzeCaptiveDependencies(currentRegistrations, serviceMap, externallyProvidedServices, endContext.ReportDiagnostic);
                AnalyzeCircularDependencies(allRegistrations, serviceMap, externallyProvidedServices, endContext.ReportDiagnostic);
            });
        });
    }

    private static void AnalyzeLocalRegistration(Registration registration, Action<Diagnostic> reportDiagnostic)
    {
        if (registration.ForType != null && !IsAssignableTo(registration.ImplementationType, registration.ForType))
        {
            reportDiagnostic(Diagnostic.Create(
                AttributeMisuse,
                registration.Location,
                $"Type '{FormatType(registration.ImplementationType)}' explicitly registers For = typeof({FormatType(registration.ForType)}), but it does not implement this type."));
        }

        if (registration.IsConcrete && registration.ForType != null)
        {
            reportDiagnostic(Diagnostic.Create(
                AttributeMisuse,
                registration.Location,
                $"Type '{FormatType(registration.ImplementationType)}' cannot specify both Concrete = true and For = typeof({FormatType(registration.ForType)})."));
        }

        if (!registration.IsCollection && registration.HasOrder && registration.Order != 0)
        {
            reportDiagnostic(Diagnostic.Create(
                AttributeMisuse,
                registration.Location,
                $"Type '{FormatType(registration.ImplementationType)}' cannot specify Order unless OfCollection = true."));
        }

        if (registration.IsCollection && registration.FactoryType != null)
        {
            reportDiagnostic(Diagnostic.Create(
                AttributeMisuse,
                registration.Location,
                $"Type '{FormatType(registration.ImplementationType)}' cannot specify both Factory and OfCollection = true."));
        }

        if (registration.FactoryType == null)
        {
            return;
        }

        var implementedFactoryTypes = GetImplementationFactoryServiceTypes(registration.FactoryType);
        if (implementedFactoryTypes.Any(type => IsSameTypeOrOriginalDefinition(type, registration.ServiceType)))
        {
            return;
        }

        reportDiagnostic(Diagnostic.Create(
            InvalidFactory,
            registration.Location,
            FormatType(registration.FactoryType),
            FormatType(registration.ServiceType)));
    }

    private static void AnalyzeDuplicateRegistrations(
        IEnumerable<Registration> currentRegistrations,
        IEnumerable<Registration> allRegistrations,
        Action<Diagnostic> reportDiagnostic)
    {
        var currentRegistrationSet = new HashSet<Registration>(currentRegistrations);

        foreach (var duplicateGroup in allRegistrations
                     .Where(registration => !registration.IsCollection && !registration.IsReplaceService)
                     .GroupBy(registration => GetTypeKey(registration.ServiceType))
                     .Where(group => group.Count() > 1))
        {
            var implementations = string.Join(", ", duplicateGroup
                .Select(registration => FormatType(registration.ImplementationType))
                .Distinct()
                .OrderBy(name => name));

            foreach (var registration in duplicateGroup.Where(currentRegistrationSet.Contains))
            {
                reportDiagnostic(Diagnostic.Create(
                    DuplicateRegistration,
                    registration.Location,
                    FormatType(registration.ServiceType),
                    implementations));
            }
        }
    }

    private static void AnalyzeDuplicateCollectionOrders(
        IEnumerable<Registration> currentRegistrations,
        IEnumerable<Registration> allRegistrations,
        Action<Diagnostic> reportDiagnostic)
    {
        var currentRegistrationSet = new HashSet<Registration>(currentRegistrations);

        foreach (var duplicateGroup in allRegistrations
                     .Where(registration => registration.IsCollection && registration.HasOrder && registration.Order != 0)
                     .GroupBy(registration => $"{GetTypeKey(registration.ServiceType)}:{registration.Order}")
                     .Where(group => group.Count() > 1))
        {
            foreach (var registration in duplicateGroup.Where(currentRegistrationSet.Contains))
            {
                reportDiagnostic(Diagnostic.Create(
                    AttributeMisuse,
                    registration.Location,
                    $"Collection registration for service '{FormatType(registration.ServiceType)}' has duplicate Order = {registration.Order}."));
            }
        }
    }

    private static void AnalyzeMissingRegistrations(
        IEnumerable<Registration> currentRegistrations,
        IReadOnlyDictionary<string, List<Registration>> serviceMap,
        ISet<string> externallyProvidedServices,
        Action<Diagnostic> reportDiagnostic)
    {
        foreach (var registration in DistinctCurrentImplementationRegistrations(currentRegistrations))
        {
            if (registration.FactoryType != null)
            {
                continue;
            }

            var missingParameters = GetMissingParameters(registration, serviceMap, externallyProvidedServices);
            foreach (var parameter in missingParameters)
            {
                reportDiagnostic(Diagnostic.Create(
                    MissingRegistration,
                    GetLocation(parameter, registration),
                    parameter.Name,
                    FormatType(registration.ImplementationType),
                    FormatType(parameter.Type)));
            }
        }
    }

    private static void AnalyzeCaptiveDependencies(
        IEnumerable<Registration> currentRegistrations,
        IReadOnlyDictionary<string, List<Registration>> serviceMap,
        ISet<string> externallyProvidedServices,
        Action<Diagnostic> reportDiagnostic)
    {
        foreach (var registration in DistinctCurrentImplementationRegistrations(currentRegistrations))
        {
            if (registration.Lifetime != RegistrationLifetime.Singleton || registration.FactoryType != null)
            {
                continue;
            }

            foreach (var dependency in GetConstructorDependencies(registration, serviceMap, externallyProvidedServices))
            {
                foreach (var dependencyRegistration in dependency.Registrations.Where(item => item.Lifetime == RegistrationLifetime.Scoped))
                {
                    reportDiagnostic(Diagnostic.Create(
                        CaptiveDependency,
                        GetLocation(dependency.Parameter, registration),
                        FormatType(registration.ImplementationType),
                        FormatType(dependency.Parameter.Type),
                        FormatType(dependencyRegistration.ImplementationType)));
                }
            }
        }
    }

    private static void AnalyzeCircularDependencies(
        IEnumerable<Registration> allRegistrations,
        IReadOnlyDictionary<string, List<Registration>> serviceMap,
        ISet<string> externallyProvidedServices,
        Action<Diagnostic> reportDiagnostic)
    {
        var registrationsByImplementation = allRegistrations
            .Where(registration => registration.FactoryType == null)
            .GroupBy(registration => GetTypeKey(registration.ImplementationType))
            .ToDictionary(group => group.Key, group => group.First());

        var graph = registrationsByImplementation.Values.ToDictionary(
            registration => GetTypeKey(registration.ImplementationType),
            registration => GetConstructorDependencies(registration, serviceMap, externallyProvidedServices)
                .SelectMany(dependency => dependency.Registrations)
                .Where(dependencyRegistration => dependencyRegistration.FactoryType == null)
                .Select(dependencyRegistration => GetTypeKey(dependencyRegistration.ImplementationType))
                .Where(key => registrationsByImplementation.ContainsKey(key))
                .Distinct()
                .ToList());

        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();
        var path = new List<string>();
        var reportedCycles = new HashSet<string>();

        foreach (var node in graph.Keys.OrderBy(key => key))
        {
            Visit(node);
        }

        void Visit(string node)
        {
            if (recursionStack.Contains(node))
            {
                return;
            }

            if (!visited.Add(node))
            {
                return;
            }

            recursionStack.Add(node);
            path.Add(node);

            foreach (var dependency in graph[node])
            {
                if (recursionStack.Contains(dependency))
                {
                    var cycleStartIndex = path.IndexOf(dependency);
                    var cycle = path.Skip(cycleStartIndex).Concat(new[] { dependency }).ToList();
                    var canonicalCycle = GetCanonicalCycleKey(cycle);
                    if (!reportedCycles.Add(canonicalCycle))
                    {
                        continue;
                    }

                    var reportRegistration = cycle
                        .Take(cycle.Count - 1)
                        .Select(key => registrationsByImplementation[key])
                        .FirstOrDefault(registration => registration.IsFromCurrentCompilation);

                    if (reportRegistration == null)
                    {
                        continue;
                    }

                    var cycleText = string.Join(" -> ", cycle.Select(key => FormatType(registrationsByImplementation[key].ImplementationType)));
                    reportDiagnostic(Diagnostic.Create(CircularDependency, reportRegistration.Location, cycleText));
                    continue;
                }

                Visit(dependency);
            }

            path.RemoveAt(path.Count - 1);
            recursionStack.Remove(node);
        }
    }

    private static IEnumerable<Registration> DistinctCurrentImplementationRegistrations(IEnumerable<Registration> registrations)
    {
        return registrations
            .Where(registration => registration.IsFromCurrentCompilation)
            .GroupBy(registration => GetTypeKey(registration.ImplementationType))
            .Select(group => group.FirstOrDefault(registration => registration.FactoryType == null) ?? group.First());
    }

    private static IReadOnlyDictionary<string, List<Registration>> BuildServiceMap(IEnumerable<Registration> registrations)
    {
        var serviceMap = new Dictionary<string, List<Registration>>();
        foreach (var registration in registrations)
        {
            var serviceKey = GetTypeKey(registration.ServiceType);
            AddRegistration(serviceKey, registration);

            if (registration.ServiceType is INamedTypeSymbol { IsGenericType: true } namedType)
            {
                var genericServiceKey = GetTypeKey(namedType.OriginalDefinition);
                if (genericServiceKey != serviceKey)
                {
                    AddRegistration(genericServiceKey, registration);
                }
            }
        }

        return serviceMap;

        void AddRegistration(string key, Registration registration)
        {
            if (!serviceMap.TryGetValue(key, out var items))
            {
                items = new List<Registration>();
                serviceMap.Add(key, items);
            }

            items.Add(registration);
        }
    }

    private static IReadOnlyList<IParameterSymbol> GetMissingParameters(
        Registration registration,
        IReadOnlyDictionary<string, List<Registration>> serviceMap,
        ISet<string> externallyProvidedServices)
    {
        var constructors = GetPublicConstructors(registration.ImplementationType);
        if (constructors.Count == 0 ||
            constructors.Any(constructor => constructor.Parameters.All(parameter => IsResolvable(parameter, serviceMap, externallyProvidedServices))))
        {
            return Array.Empty<IParameterSymbol>();
        }

        return constructors[0].Parameters
            .Where(parameter => !IsResolvable(parameter, serviceMap, externallyProvidedServices))
            .ToArray();
    }

    private static IReadOnlyList<ConstructorDependency> GetConstructorDependencies(
        Registration registration,
        IReadOnlyDictionary<string, List<Registration>> serviceMap,
        ISet<string> externallyProvidedServices)
    {
        var constructors = GetPublicConstructors(registration.ImplementationType);
        if (constructors.Count == 0)
        {
            return Array.Empty<ConstructorDependency>();
        }

        var selectedConstructor = constructors
            .FirstOrDefault(constructor => constructor.Parameters.All(parameter => IsResolvable(parameter, serviceMap, externallyProvidedServices))) ??
                                  constructors[0];

        return selectedConstructor.Parameters
            .Select(parameter => new ConstructorDependency(parameter, ResolveRegistrations(parameter.Type, serviceMap)))
            .Where(dependency => dependency.Registrations.Count > 0)
            .ToArray();
    }

    private static List<IMethodSymbol> GetPublicConstructors(INamedTypeSymbol implementationType)
    {
        return implementationType.InstanceConstructors
            .Where(constructor => constructor.DeclaredAccessibility == Accessibility.Public && !constructor.IsStatic)
            .OrderByDescending(constructor => constructor.Parameters.Length)
            .ThenBy(constructor => constructor.ToDisplayString())
            .ToList();
    }

    private static bool IsResolvable(
        IParameterSymbol parameter,
        IReadOnlyDictionary<string, List<Registration>> serviceMap,
        ISet<string> externallyProvidedServices)
    {
        return parameter.HasExplicitDefaultValue ||
               IsFrameworkProvided(parameter.Type, serviceMap) ||
               IsExternallyProvided(parameter.Type, externallyProvidedServices) ||
               ResolveRegistrations(parameter.Type, serviceMap).Count > 0;
    }

    private static IReadOnlyList<Registration> ResolveRegistrations(
        ITypeSymbol type,
        IReadOnlyDictionary<string, List<Registration>> serviceMap)
    {
        if (TryGetCollectionItemType(type, out var collectionItemType))
        {
            return ResolveRegistrations(collectionItemType, serviceMap);
        }

        foreach (var key in GetResolutionKeys(type))
        {
            if (serviceMap.TryGetValue(key, out var registrations))
            {
                return registrations;
            }
        }

        return Array.Empty<Registration>();
    }

    private static bool IsFrameworkProvided(ITypeSymbol type, IReadOnlyDictionary<string, List<Registration>> serviceMap)
    {
        if (TryGetCollectionItemType(type, out var collectionItemType))
        {
            return ResolveRegistrations(collectionItemType, serviceMap).Count > 0;
        }

        return type is INamedTypeSymbol namedType &&
               (IsMetadataType(namedType, "System.IServiceProvider") ||
                IsMetadataType(namedType, "Microsoft.Extensions.DependencyInjection.IServiceScopeFactory") ||
                IsMetadataType(namedType, "Microsoft.Extensions.Logging.ILogger`1") ||
                IsMetadataType(namedType, "Microsoft.Extensions.Options.IOptions`1") ||
                IsMetadataType(namedType, "Microsoft.Extensions.Options.IOptionsMonitor`1") ||
                IsMetadataType(namedType, "Microsoft.Extensions.Options.IOptionsSnapshot`1") ||
                IsMetadataType(namedType, "System.Net.Http.IHttpClientFactory") ||
                IsMetadataType(namedType, "Microsoft.Extensions.Configuration.IConfiguration") ||
                IsMetadataType(namedType, "Microsoft.Extensions.Hosting.IHostEnvironment") ||
                IsMetadataType(namedType, "Microsoft.AspNetCore.Hosting.IWebHostEnvironment") ||
                IsMetadataType(namedType, "Microsoft.Extensions.Caching.Memory.IMemoryCache") ||
                IsMetadataType(namedType, "System.Lazy`1"));
    }

    private static bool IsExternallyProvided(ITypeSymbol type, ISet<string> externallyProvidedServices)
    {
        return GetResolutionKeys(type).Any(externallyProvidedServices.Contains);
    }

    private static bool TryGetCollectionItemType(ITypeSymbol type, out ITypeSymbol itemType)
    {
        if (type is INamedTypeSymbol namedType &&
            namedType.TypeArguments.Length == 1 &&
            (namedType.ConstructedFrom.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T ||
             IsMetadataType(namedType, "System.Collections.Generic.IReadOnlyList`1")))
        {
            itemType = namedType.TypeArguments[0];
            return true;
        }

        itemType = null;
        return false;
    }

    private static ISet<string> GetExternallyProvidedServices(Compilation compilation)
    {
        var services = new HashSet<string>();
        foreach (var assembly in GetAssembliesToInspect(compilation, includeCurrentAssembly: true))
        {
            foreach (var attribute in assembly.GetAttributes())
            {
                if (attribute.AttributeClass == null ||
                    !IsMetadataType(attribute.AttributeClass, ExternallyProvidedAttributeName) ||
                    attribute.ConstructorArguments.Length != 1 ||
                    attribute.ConstructorArguments[0].Value is not ITypeSymbol serviceType)
                {
                    continue;
                }

                foreach (var key in GetResolutionKeys(serviceType))
                {
                    services.Add(key);
                }
            }
        }

        return services;
    }

    private static IEnumerable<Registration> GetReferencedRegistrations(Compilation compilation)
    {
        foreach (var assembly in GetAssembliesToInspect(compilation, includeCurrentAssembly: false))
        {
            if (!ReferencesAbstractions(assembly))
            {
                continue;
            }

            foreach (var type in GetNamedTypes(assembly.GlobalNamespace))
            {
                foreach (var registration in GetRegistrations(type, isFromCurrentCompilation: false))
                {
                    yield return registration;
                }
            }
        }
    }

    private static IEnumerable<IAssemblySymbol> GetAssembliesToInspect(Compilation compilation, bool includeCurrentAssembly)
    {
        if (includeCurrentAssembly)
        {
            yield return compilation.Assembly;
        }

        var seen = new HashSet<string>();
        foreach (var assembly in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            if (seen.Add(assembly.Identity.GetDisplayName()))
            {
                yield return assembly;
            }
        }
    }

    private static bool ReferencesAbstractions(IAssemblySymbol assembly)
    {
        return assembly.Modules
            .SelectMany(module => module.ReferencedAssemblySymbols)
            .Any(referencedAssembly => referencedAssembly.Identity.Name == AbstractionsAssemblyName);
    }

    private static IEnumerable<INamedTypeSymbol> GetNamedTypes(INamespaceSymbol namespaceSymbol)
    {
        foreach (var type in namespaceSymbol.GetTypeMembers())
        {
            foreach (var nestedType in GetNamedTypes(type))
            {
                yield return nestedType;
            }
        }

        foreach (var childNamespace in namespaceSymbol.GetNamespaceMembers())
        {
            foreach (var type in GetNamedTypes(childNamespace))
            {
                yield return type;
            }
        }
    }

    private static IEnumerable<INamedTypeSymbol> GetNamedTypes(INamedTypeSymbol type)
    {
        yield return type;

        foreach (var nestedType in type.GetTypeMembers())
        {
            foreach (var childType in GetNamedTypes(nestedType))
            {
                yield return childType;
            }
        }
    }

    private static IEnumerable<Registration> GetRegistrations(INamedTypeSymbol implementationType, bool isFromCurrentCompilation)
    {
        foreach (var attribute in implementationType.GetAttributes())
        {
            if (attribute.AttributeClass == null ||
                !Constants.AttributeRegistrationTypes.TryGetValue(attribute.AttributeClass.ToDisplayString(), out var registrationType))
            {
                continue;
            }

            var registrationProperties = GetRegistrationProperties(attribute);
            var serviceType = GetServiceType(implementationType, registrationProperties);
            yield return new Registration(
                implementationType,
                serviceType,
                GetLifetime(registrationType),
                attribute,
                registrationProperties.ForType,
                registrationProperties.FactoryType,
                registrationProperties.IsConcrete,
                registrationProperties.IsCollection,
                registrationProperties.IsReplaceService,
                registrationProperties.HasOrder,
                registrationProperties.Order,
                isFromCurrentCompilation);
        }
    }

    private static RegistrationProperties GetRegistrationProperties(AttributeData attribute)
    {
        var properties = new RegistrationProperties();
        foreach (var namedArgument in attribute.NamedArguments)
        {
            switch (namedArgument.Key)
            {
                case "For" when namedArgument.Value.Value is ITypeSymbol forType:
                    properties.ForType = forType;
                    break;
                case "Factory" when namedArgument.Value.Value is ITypeSymbol factoryType:
                    properties.FactoryType = factoryType;
                    break;
                case "Concrete" when namedArgument.Value.Value is bool isConcrete:
                    properties.IsConcrete = isConcrete;
                    break;
                case "ReplaceService" when namedArgument.Value.Value is bool isReplaceService:
                    properties.IsReplaceService = isReplaceService;
                    break;
                case "OfCollection" when namedArgument.Value.Value is bool isCollection:
                    properties.IsCollection = isCollection;
                    break;
                case "Order" when namedArgument.Value.Value is int order:
                    properties.HasOrder = true;
                    properties.Order = order;
                    break;
            }
        }

        return properties;
    }

    private static ITypeSymbol GetServiceType(INamedTypeSymbol implementationType, RegistrationProperties properties)
    {
        if (properties.IsConcrete)
        {
            return implementationType;
        }

        if (properties.ForType != null)
        {
            return properties.ForType;
        }

        return implementationType.Interfaces.FirstOrDefault() ?? implementationType;
    }

    private static RegistrationLifetime GetLifetime(RegistrationType registrationType)
    {
        return registrationType switch
        {
            RegistrationType.Singleton => RegistrationLifetime.Singleton,
            RegistrationType.HostedService => RegistrationLifetime.Singleton,
            RegistrationType.Scoped => RegistrationLifetime.Scoped,
            _ => RegistrationLifetime.Transient
        };
    }

    private static IReadOnlyList<ITypeSymbol> GetImplementationFactoryServiceTypes(ITypeSymbol factoryType)
    {
        if (factoryType is not INamedTypeSymbol namedFactoryType)
        {
            return Array.Empty<ITypeSymbol>();
        }

        return namedFactoryType.AllInterfaces
            .Where(type => IsMetadataType(type, ImplementationFactoryMetadataName))
            .Select(type => type.TypeArguments[0])
            .ToArray();
    }

    private static bool IsAssignableTo(INamedTypeSymbol implementationType, ITypeSymbol serviceType)
    {
        if (IsSameTypeOrOriginalDefinition(implementationType, serviceType))
        {
            return true;
        }

        if (implementationType.AllInterfaces.Any(type => IsSameTypeOrOriginalDefinition(type, serviceType)))
        {
            return true;
        }

        var baseType = implementationType.BaseType;
        while (baseType != null)
        {
            if (IsSameTypeOrOriginalDefinition(baseType, serviceType))
            {
                return true;
            }

            baseType = baseType.BaseType;
        }

        return false;
    }

    private static bool IsSameTypeOrOriginalDefinition(ITypeSymbol candidate, ITypeSymbol expected)
    {
        if (SymbolEqualityComparer.Default.Equals(candidate, expected))
        {
            return true;
        }

        return candidate is INamedTypeSymbol candidateNamed &&
               expected is INamedTypeSymbol expectedNamed &&
               SymbolEqualityComparer.Default.Equals(candidateNamed.OriginalDefinition, expectedNamed.OriginalDefinition);
    }

    private static bool IsMetadataType(INamedTypeSymbol type, string metadataName)
    {
        return GetFullMetadataName(type.OriginalDefinition) == metadataName;
    }

    private static string GetFullMetadataName(INamedTypeSymbol type)
    {
        var typeParts = new Stack<string>();
        var currentType = type;
        while (currentType != null)
        {
            typeParts.Push(currentType.MetadataName);
            currentType = currentType.ContainingType;
        }

        var typeName = string.Join(".", typeParts);
        if (type.ContainingNamespace == null || type.ContainingNamespace.IsGlobalNamespace)
        {
            return typeName;
        }

        return $"{type.ContainingNamespace.ToDisplayString()}.{typeName}";
    }

    private static IEnumerable<string> GetResolutionKeys(ITypeSymbol type)
    {
        yield return GetTypeKey(type);

        if (type is INamedTypeSymbol { IsGenericType: true } namedType)
        {
            yield return GetTypeKey(namedType.OriginalDefinition);
        }
    }

    private static string GetTypeKey(ITypeSymbol type)
    {
        return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    private static string FormatType(ITypeSymbol type)
    {
        return type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
    }

    private static Location GetLocation(IParameterSymbol parameter, Registration registration)
    {
        return parameter.Locations.FirstOrDefault(location => location.IsInSource) ?? registration.Location;
    }

    private static string GetCanonicalCycleKey(IReadOnlyList<string> cycle)
    {
        return string.Join("|", cycle.Take(cycle.Count - 1).OrderBy(key => key));
    }
}
