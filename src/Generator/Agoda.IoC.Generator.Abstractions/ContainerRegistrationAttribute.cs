﻿namespace Agoda.IoC.Generator.Abstractions;

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
    /// <summary>
    /// By default, any non-BCL base class and interfaces will be registered. Use this property to specify exactly one.
    /// </summary>
    /// <example>
    /// [RegisterSingleton(For = typeof(IInterface2)]
    /// public class MyClass : IInterface1, IInterface2 { ... }
    /// </example>
    public Type For { get; set; }

    /// <summary>
    /// Specifies a class used to build the component. This is useful when custom setup is required. The factory must
    /// implement IComponentFactory&lt;T&gt;, where T is the type of the injected component.
    /// </summary>
    /// <remarks>
    /// While they do serve a legitimate purpose, usage of factories is discouraged in the following situations:
    /// - To choose between multiple constructors. A better design limits injectables to a single constructor and
    /// use of the null-object pattern for truly optional dependencies.
    /// - To provide base types such as strings and ints to your component, often from AppSettings. A better design
    /// is to store the settings in Consul and inject the Consul class representing the setting, or an aggregate of
    /// such settings. 
    /// Factory cannot be used with OfCollection = true or Key.
    /// </remarks>
    public Type Factory { get; set; }

    /// <summary>
    /// Registers the type as a concrete implementation. Does not register the base class or any implemented interfaces. 
    /// </summary>
    /// <example>  
    /// [RegisterSingleton(Concrete = true)]
    /// public class InjectMe : ICannotBeInjected { ... }
    /// </example>
    public bool Concrete { get; set; }

    public bool ReplaceService { get; set; }

    /// <summary>
    /// To prevent hard to find bugs and unexpected behavior, re-registering the same type is disallowed and will
    /// throw an exception. If you would like to register multiple implementations of the same type as a collection,
    /// set this to true.
    /// </summary>
    /// <remarks>
    /// The order in which the items of a collection are resolved can be defined with the Order property. If this
    /// is not set then resolution order is not guaranteed.
    /// See also Key property.
    /// </remarks>
    /// <example>
    /// [RegisterSingleton(OfCollection = true)]
    /// public class MyCollectionItem1 : ICollectionItem { ... }
    /// </example>
    public bool OfCollection { get; set; }

    /// <summary>
    /// Defines the order in which items appear in a resolved collection.
    /// </summary>
    /// <remarks>
    /// Can only be used when OfCollection = true. Lower numbers appear in the resolved collection before higher numbers.
    /// If omitted then its position in the collection is undefined.  
    /// </remarks>
    /// <example>
    /// [RegisterSingleton(OfCollection = true, Order = 1)]
    /// public class MyCollectionItem1 : ICollectionItem { ... }
    /// [RegisterSingleton(OfCollection = true, Order = 2)]
    /// public class MyCollectionItem2 : ICollectionItem { ... }
    /// </example>
    public int Order { get; set; }
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



/// <inheritdoc />
/// <summary>
/// Add an <see cref="IHostedService"/> registration for the given type.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class RegisterHostedServiceAttribute : ContainerRegistration { }

public interface IImplementationFactory<T>
{
    T Factory(IServiceProvider serviceProvider);
}