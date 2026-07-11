using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Agoda.IoC.Generator.Analyzers;

internal static class Diagnostics
{
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

    internal static readonly ImmutableArray<DiagnosticDescriptor> All =
        ImmutableArray.Create(
            MissingRegistration,
            CaptiveDependency,
            DuplicateRegistration,
            InvalidFactory,
            AttributeMisuse,
            CircularDependency);
}
