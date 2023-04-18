namespace Agoda.IoC.Generator.Abstractions;

/// <inheritdoc />
/// <summary>
/// Decorate your class with one of these attributes to automatically register with the DI container.
/// </summary>
/// <example>
/// [RegisterSingleton]
/// public class MyService : MyBaseService, IMyInterface, IMyInterface2 { ... } 
/// </example>
/// <remarks>
/// Instructs the container to resolve MyBaseService, IMyInterface, IMyInterface2, and all other base classes and
/// interfaces recursively to MyService.
/// </remarks>
public abstract class ContainerRegistration : Attribute
{
    public Type For { get; set; }
    public Type Factory { get; set; }
    public bool Concrete { get; set; }
    public bool ReplaceService { get; set; }
}

/// <inheritdoc />
/// <summary>
/// A single instance will be built by the container when first resolved, and reused for the lifetime of the app.
/// This is the most efficient lifestyle, and you should aim for singletons when possible. 
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class RegisterSingletonAttribute : ContainerRegistration { }

/// <inheritdoc />
/// <summary>
/// A single instance will be built for each HTTP request when first resolved, and reused for the
/// lifetime of that request.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class RegisterScopedAttribute : ContainerRegistration { }

/// <inheritdoc />
/// <summary>
/// A single instance will be built for each HTTP request when first resolved, and reused for the
/// lifetime of that request.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class RegisterPerRequestAttribute : ContainerRegistration { }

/// <inheritdoc />
/// <summary>
/// A new instance will be built by the container *every time* the component is resolved. This is clearly the least
/// efficient lifestyle, and its use-cases are minimal. Only use this lifestyle if you have a good reason.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class RegisterTransientAttribute : ContainerRegistration { }

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class RegisterHostedServiceAttribute : ContainerRegistration { }


public interface IImplementationFactory<T>
{
    T Factory(IServiceProvider serviceProvider);
}