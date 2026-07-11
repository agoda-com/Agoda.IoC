namespace Agoda.IoC.Generator.Abstractions;

/// <summary>
/// Declares that a service type is intentionally registered outside the Agoda.IoC.Generator attribute model.
/// </summary>
/// <example>
/// [assembly: ExternallyProvided(typeof(IFeatureFlagClient))]
/// </example>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class ExternallyProvidedAttribute : Attribute
{
    /// <summary>
    /// Declares a service type that is resolved by manual registration, framework registration, or another container extension.
    /// </summary>
    public ExternallyProvidedAttribute(Type serviceType) => ServiceType = serviceType;

    /// <summary>
    /// The service type that is intentionally provided outside Agoda.IoC.Generator registrations.
    /// </summary>
    public Type ServiceType { get; }
}
