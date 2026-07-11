using Microsoft.CodeAnalysis;

namespace Agoda.IoC.Generator.Analyzers;

internal sealed class ServiceGraph
{
    private static readonly HashSet<string> FrameworkProvidedTypes = new(StringComparer.Ordinal)
    {
        "System.IServiceProvider",
        "Microsoft.Extensions.DependencyInjection.IServiceScopeFactory",
        "Microsoft.Extensions.Logging.ILogger`1",
        "Microsoft.Extensions.Options.IOptions`1",
        "Microsoft.Extensions.Options.IOptionsMonitor`1",
        "Microsoft.Extensions.Options.IOptionsSnapshot`1",
        "System.Net.Http.IHttpClientFactory",
        "Microsoft.Extensions.Configuration.IConfiguration",
        "Microsoft.Extensions.Hosting.IHostEnvironment",
        "Microsoft.AspNetCore.Hosting.IWebHostEnvironment",
        "Microsoft.Extensions.Caching.Memory.IMemoryCache",
        "System.Lazy`1"
    };

    private readonly ISet<string> _externallyProvidedServices;
    private readonly IReadOnlyDictionary<string, List<Registration>> _serviceMap;

    internal ServiceGraph(
        IEnumerable<Registration> currentRegistrations,
        IEnumerable<Registration> allRegistrations,
        ISet<string> externallyProvidedServices)
    {
        Current = currentRegistrations.ToArray();
        All = allRegistrations.ToArray();
        _externallyProvidedServices = new HashSet<string>(externallyProvidedServices, StringComparer.Ordinal);
        _serviceMap = BuildServiceMap(All);
    }

    internal IReadOnlyList<Registration> All { get; }

    internal IReadOnlyList<Registration> Current { get; }

    internal IReadOnlyList<Registration> Resolve(ITypeSymbol type)
    {
        if (TryGetCollectionItemType(type, out var collectionItemType))
        {
            return Resolve(collectionItemType);
        }

        foreach (var key in type.GetResolutionKeys())
        {
            if (_serviceMap.TryGetValue(key, out var registrations))
            {
                return registrations;
            }
        }

        return Array.Empty<Registration>();
    }

    internal bool IsResolvable(IParameterSymbol parameter)
    {
        return parameter.HasExplicitDefaultValue ||
               IsFrameworkProvided(parameter.Type) ||
               IsExternallyProvided(parameter.Type) ||
               Resolve(parameter.Type).Count > 0;
    }

    internal IReadOnlyList<ConstructorDependency> GetConstructorDependencies(Registration registration)
    {
        var constructors = GetPublicConstructors(registration.ImplementationType);
        if (constructors.Count == 0)
        {
            return Array.Empty<ConstructorDependency>();
        }

        var selectedConstructor = constructors
            .FirstOrDefault(constructor => constructor.Parameters.All(IsResolvable)) ?? constructors[0];

        return selectedConstructor.Parameters
            .Select(parameter => new ConstructorDependency(parameter, Resolve(parameter.Type)))
            .Where(dependency => dependency.Registrations.Count > 0)
            .ToArray();
    }

    internal IReadOnlyList<IParameterSymbol> GetMissingParameters(Registration registration)
    {
        var constructors = GetPublicConstructors(registration.ImplementationType);
        if (constructors.Count == 0 ||
            constructors.Any(constructor => constructor.Parameters.All(IsResolvable)))
        {
            return Array.Empty<IParameterSymbol>();
        }

        return constructors[0].Parameters
            .Where(parameter => !IsResolvable(parameter))
            .ToArray();
    }

    internal IEnumerable<Registration> GetDistinctCurrentImplementationRegistrations()
    {
        return Current
            .Where(registration => registration.IsFromCurrentCompilation)
            .GroupBy(registration => registration.ImplementationType.GetTypeKey())
            .Select(group => group.FirstOrDefault(registration => registration.FactoryType == null) ?? group.First());
    }

    private static IReadOnlyDictionary<string, List<Registration>> BuildServiceMap(IEnumerable<Registration> registrations)
    {
        var serviceMap = new Dictionary<string, List<Registration>>(StringComparer.Ordinal);
        foreach (var registration in registrations)
        {
            var serviceKey = registration.ServiceType.GetTypeKey();
            AddRegistration(serviceKey, registration);

            if (registration.ServiceType is INamedTypeSymbol { IsGenericType: true } namedType)
            {
                var genericServiceKey = namedType.OriginalDefinition.GetTypeKey();
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

    private static List<IMethodSymbol> GetPublicConstructors(INamedTypeSymbol implementationType)
    {
        return implementationType.InstanceConstructors
            .Where(constructor => constructor.DeclaredAccessibility == Accessibility.Public && !constructor.IsStatic)
            .OrderByDescending(constructor => constructor.Parameters.Length)
            .ThenBy(constructor => constructor.ToDisplayString())
            .ToList();
    }

    private bool IsFrameworkProvided(ITypeSymbol type)
    {
        if (TryGetCollectionItemType(type, out var collectionItemType))
        {
            return Resolve(collectionItemType).Count > 0;
        }

        return type is INamedTypeSymbol namedType &&
               FrameworkProvidedTypes.Contains(namedType.OriginalDefinition.GetFullMetadataName());
    }

    private bool IsExternallyProvided(ITypeSymbol type)
    {
        return type.GetResolutionKeys().Any(_externallyProvidedServices.Contains);
    }

    private static bool TryGetCollectionItemType(ITypeSymbol type, out ITypeSymbol itemType)
    {
        if (type is INamedTypeSymbol namedType &&
            namedType.TypeArguments.Length == 1 &&
            (namedType.ConstructedFrom.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T ||
             namedType.IsMetadataType("System.Collections.Generic.IReadOnlyList`1")))
        {
            itemType = namedType.TypeArguments[0];
            return true;
        }

        itemType = null;
        return false;
    }
}
