using System;

namespace Agoda.IoC.Core
{
    /// <inheritdoc />
    /// <summary>
    /// Decorate an assembly with this attribute to enable its ContainerRegistrationAttribute decorated classes to be
    /// scanned and automatically registered with the DI container. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class EnableContainerRegistrationsByAttributeAttribute : Attribute
    {
    }

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
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public abstract class ContainerRegistrationAttribute : Attribute
    {   
        /// <summary>
        /// Optionally specify an alternative implementation to use under mock mode. Cannot be used for concrete registrations.
        /// </summary>
        /// <example>
        /// [RegisterSingleton(Mock = typeof(MyMockService)]
        /// public class MyService : IMyService { ... }
        /// </example>
        public Type Mock { get; set; }

        /// <summary>
        /// By default, any non-BCL base class and interfaces will be registered. Use this property to specify exactly one.
        /// </summary>
        /// <example>
        /// [RegisterSingleton(For = typeof(IInterface2)]
        /// public class MyClass : IInterface1, IInterface2 { ... }
        /// </example>
        public Type For { get; set; }
        
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
        
        /// <summary>
        /// Registers the type as a concrete implementation. Does not register the base class or any implemented interfaces. 
        /// </summary>
        /// <example>
        /// [RegisterSingleton(Concrete = true)]
        /// public class InjectMe : ICannotBeInjected { ... }
        /// </example>
        public bool Concrete { get; set; }

        /// <summary>
        /// Registers a type to be resolved by a unique key. Results in the registration of an IKeyedComponentFactory&lt;T&gt;
        /// - where T is the target type to be resolved. The IKeyedComponentFactory&lt;T&gt; should be injected into your
        /// component.
        /// </summary>
        /// <remarks>
        /// Key can be of any compile-time constant type.
        /// ToString() will be called on the key.
        /// Key cannot be used with OfCollection = true or Factory. 
        /// See also OfCollection property.
        /// </remarks>
        /// <example>
        /// [RegisterSingleton(Key = TransportProtocols.Http)] // enum
        /// public class HttpTransport : ITransportProtocol { ... }
        /// 
        /// [RegisterSingleton(Key = TransportConstants.HTTP)] // constant
        /// public class HttpTransport : ITransportProtocol { ... }
        /// 
        /// [RegisterSingleton(Key = "http")] // string
        /// public class HttpTransport : ITransportProtocol { ... }
        /// 
        /// Then, to resolve in your service:
        /// public MyService(IKeyedComponentFactory&lt;ITransportProtocol&gt; protocolFactory)
        /// {
        ///     var httpTransport = protocolFactory.GetByKey(TransportProtocols.Http);
        /// }
        /// </example>
        public object Key { get; set; }

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
        /// When intercepting or building a generic type by factory we must provide the concrete type being built.
        /// This is only required when using Factory or interceptors with a generic type.
        /// </summary>
        /// <remarks>
        /// Only types of a single generic argument are supported, which is enough for our use case.
        /// </remarks>
        public Type GenericArgument { get; set; }

        /// <summary>
        /// ReplaceServices: Set true to replace services if they are already registered before. Uses Replace extension method of IServiceCollection.
        /// </summary>
        public bool ReplaceServices { get; set; }

        //[EditorBrowsable(EditorBrowsableState.Never)] // uncomment this once all registrations are migrated
        [Obsolete("Use only for legacy registrations migrated from the RegisterRepository() or RegisterService() Unity " +
                  "extensions. For new code use ServerSideTimer.")]
        public bool LegacyMeasured { get; set; }
    }

    /// <inheritdoc />
    /// <summary>
    /// A single instance will be built by the container when first resolved, and reused for the lifetime of the app.
    /// This is the most efficient lifestyle, and you should aim for singletons when possible. 
    /// </summary>
    public class RegisterSingletonAttribute : ContainerRegistrationAttribute
    {
    }

    /// <inheritdoc />
    /// <summary>
    /// A single instance will be built for each HTTP request when first resolved, and reused for the
    /// lifetime of that request.
    /// </summary>
    public class RegisterPerRequestAttribute : ContainerRegistrationAttribute
    {
    }

    /// <inheritdoc />
    /// <summary>
    /// A new instance will be built by the container *every time* the component is resolved. This is clearly the least
    /// efficient lifestyle, and its use-cases are minimal. Only use this lifestyle if you have a good reason.
    /// </summary>
    public class RegisterTransientAttribute : ContainerRegistrationAttribute
    {
    }
}
