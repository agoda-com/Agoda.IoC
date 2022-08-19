using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Agoda.IoC.Core;
using Microsoft.Practices.Unity;

namespace Agoda.IoC.Unity
{
    /// <summary>
    /// Registers types decorated with a ContainerRegistrationAttribute.
    /// </summary>
    public static class UnityContainerAttributeExtensions
    {
        /// <summary>
        /// We need to keep track of the items registered for collections so we can resolve them in the correct order.
        /// </summary>
        private static IDictionary<Type, List<ResolutionItem>> _collectionItemsForType;

        /// <summary>
        /// We need to ensure that keys in a KeyedCollection are unique. We keep track of them here.
        /// </summary>
        private static IDictionary<Type, HashSet<string>> _keysForTypes;

        private static IUnityContainer _container;
        private static ContainerInterceptorWrapper _interceptorWrapper;
        private static IComponentResolver _componentResolver;

        public static void RegisterByAttribute(this IUnityContainer container, bool mockMode, params Assembly[] assemblies)
        {
            _container = container;
            _collectionItemsForType = new Dictionary<Type, List<ResolutionItem>>();
            _keysForTypes = new Dictionary<Type, HashSet<string>>();
            _interceptorWrapper = CreateInterceptorWrapper();
            _componentResolver = new UnityComponentResolver(container);

            var scanAndRegisterActions = new List<IList<string>>
            {
                ScanAndRegister<RegisterSingletonAttribute, ContainerControlledLifetimeManager>(mockMode, assemblies),
                ScanAndRegister<RegisterPerRequestAttribute, HttpContextLifetimeManager>(mockMode, assemblies),
                ScanAndRegister<RegisterTransientAttribute, TransientLifetimeManager>(mockMode, assemblies),
            };

            var errorMessages = scanAndRegisterActions.SelectMany(msg => msg).ToList();
            ContainerAttributeUtils.ThrowIfError(errorMessages);
        }

        /// <summary>
        /// Scans the given assemblies for classes decorated with a ContainerRegistrationAttribute. When found, attempts
        /// to register the component with the container.
        /// </summary>
        /// <param name="mockMode">Determines if we should register the mock implementation</param>
        /// <param name="assemblies">The assemblies to scan</param>
        /// <typeparam name="TAttribute">The type of the attribute</typeparam>
        /// <typeparam name="TLifestyleManager">The container lifetime to which this attribute corresponds</typeparam>
        /// <returns>A list of error messages encountered during registration validation, or an empty list if all were successful.</returns>
        private static IList<string> ScanAndRegister<TAttribute, TLifestyleManager>(bool mockMode, Assembly[] assemblies)
            where TAttribute : ContainerRegistrationAttribute
            where TLifestyleManager : LifetimeManager, new()
        {
            // Look for all classes in the given assemblies that are decorated with a ContainerRegistrationAttribute
            var registrations = assemblies
                .SelectMany(assembly => AssemblyHelper.GetAllTypes(assembly))
                .Where(type => type.IsClass)
                .Select(type => new
                {
                    ToType = type,
                    Attributes = type.GetCustomAttributes<TAttribute>(false)
                })
                .Where(x => x.Attributes.Any())
                .SelectMany(x => x.Attributes.Select(
                    attr => new RegistrationContext(attr, x.ToType, mockMode, _interceptorWrapper)
                 ))
                .ToList();

            var errorMsgs = registrations
                .Where(reg => !reg.Validation.IsValid)
                .Select(reg => reg.Validation.ErrorMessage)
                .ToList();
            if (errorMsgs.Any())
            {
                return errorMsgs;
            }

            foreach (var reg in registrations)
            {
                try
                {
                    if (reg.FromType != null)
                    {
                        RegisterAbstract<TLifestyleManager>(reg, mockMode);
                    }
                    else
                    {
                        RegisterConcrete<TLifestyleManager>(reg);
                    }
                }
                catch (RegistrationFailedException ex)
                {
                    errorMsgs.Add(ex.Message);
                }
            }

            return errorMsgs;
        }

        private static void RegisterAbstract<TLifestyleManager>(RegistrationContext reg, bool mockMode)
            where TLifestyleManager : LifetimeManager, new()
        {
            var injectionMembers = new List<InjectionMember>();
            var toType = mockMode && reg.MockType != null
                ? reg.MockType
                : reg.ToType;

            // Set up supporting registrations.

            if (reg.Collection.IsCollection)
            {
                RegisterForCollection<TLifestyleManager>(reg);
            }
            else if (reg.Key != null)
            {
                EnsureKeyUnique(reg);
                RegisterKeyedFactory(reg);
            }
            else if (reg.FactoryType != null)
            {
                EnsureNotAlreadyRegistered(reg);
                injectionMembers.Add(new InjectionFactory(BuildFactory(reg)));
            }
            else
            {
                EnsureNotAlreadyRegistered(reg);
            }

            // Register the component itself.

            // factories wrap interceptors as they build their components, so we don't do that here
            if (reg.IsIntercepted && reg.FactoryType == null)
            {
                object WrapInterceptors(IUnityContainer c) => _interceptorWrapper.WrapInterceptors(c.Resolve(toType), reg);
                _container.RegisterType(reg.FromType, reg.Key, new TLifestyleManager(), new InjectionFactory(WrapInterceptors));
            }
            else
            {
                _container.RegisterType(reg.FromType, toType, reg.Key, new TLifestyleManager(), injectionMembers.ToArray());
            }
        }

        private static Func<IUnityContainer, object> BuildFactory(RegistrationContext reg)
        {
            var factoryInstance = Activator.CreateInstance(reg.FactoryType);
            var buildMethod = factoryInstance.GetType().GetMethod("Build");
            Debug.Assert(buildMethod != null, nameof(buildMethod) + " != null"); // type is checked by RegistrationInfo.Validate()
            var fastBuildMethod = new FastMethodInfo(buildMethod);
            var componentResolverInstance = Activator.CreateInstance(typeof(UnityComponentResolver), _container);

            return c =>
            {
                var instance = fastBuildMethod.Invoke(factoryInstance, componentResolverInstance);
                return _interceptorWrapper.WrapInterceptors(instance, reg);
            };
        }

        private static void RegisterKeyedFactory(RegistrationContext reg)
        {
            var keyedFactoryInterfaceType = typeof(IKeyedComponentFactory<>).MakeGenericType(reg.FromType);
            var keyedFactoryImplementationType = typeof(KeyedComponentFactory<>).MakeGenericType(reg.FromType);

            // obtain the keyed factory for this type from the container, or create and register one if it does not yet exist
            object keyedFactoryInstance;
            if (_container.IsRegistered(keyedFactoryInterfaceType))
            {
                keyedFactoryInstance = _container.Resolve(keyedFactoryInterfaceType);
            }
            else
            {
                var componentResolverType = typeof(UnityKeyedComponentResolver<>).MakeGenericType(reg.FromType);
                var componentResolverInstance = Activator.CreateInstance(componentResolverType, _container);
                keyedFactoryInstance = Activator.CreateInstance(keyedFactoryImplementationType, componentResolverInstance);
                _container.RegisterInstance(keyedFactoryInterfaceType, keyedFactoryInstance);
            }

            // tell the keyed factory about the new key
            var registerKeyMethod = keyedFactoryInstance.GetType().GetMethod("RegisterKey");
            Debug.Assert(registerKeyMethod != null, nameof(registerKeyMethod) + " != null");
            registerKeyMethod.Invoke(keyedFactoryInstance, new object[] { reg.Key });
        }

        private static void RegisterConcrete<TLifestyleManager>(RegistrationContext reg)
            where TLifestyleManager : LifetimeManager, new()
        {
            EnsureNotAlreadyRegistered(reg);
            var injectionMembers = new List<InjectionMember>();

            if (reg.FactoryType != null)
            {
                var factory = BuildFactory(reg);
                injectionMembers.Add(new InjectionFactory(factory));
            }

            _container.RegisterType(reg.ToType, new TLifestyleManager(), injectionMembers.ToArray());
        }

        private static void RegisterForCollection<TLifestyleManager>(RegistrationContext reg)
            where TLifestyleManager : LifetimeManager, new()
        {
            reg.Key = Guid.NewGuid().ToString();

            // As well as registering the collection items individually, Unity also requires us to explicitly register
            // the IEnumerable<T> to make them injectable as a collection. Do this now if we haven't already done so.
            var enumerableInterface = typeof(IEnumerable<>).MakeGenericType(reg.FromType);
            if (!_container.IsRegistered(enumerableInterface))
            {
                _container.RegisterType(enumerableInterface, new TLifestyleManager(), new InjectionFactory(c =>
                {
                    // To keep the runtime happy, we need to return a strongly typed enumerable. The only way I can see
                    // to do this with the runtime types we have available is by calling Array.CreateInstance with our
                    // target type and copying into it.
                    var untypedObjects = _collectionItemsForType[reg.FromType]
                        .OrderBy(item => item.Order)
                        .Select(item => c.Resolve(reg.FromType, item.Key))
                        .ToArray();
                    var typedObjects = Array.CreateInstance(reg.FromType, untypedObjects.Length);
                    Array.Copy(untypedObjects, typedObjects, untypedObjects.Length);
                    return typedObjects;
                }));

                _collectionItemsForType.Add(reg.FromType, new List<ResolutionItem>());
            }

            // Ensure Order is unique for collection.
            if (reg.Collection.Order != 0)
            {
                var registeredOrders = _collectionItemsForType[reg.FromType]
                    .Where(item => item.Order != 0)
                    .OrderBy(item => item.Order)
                    .ToList();
                if (registeredOrders.Any(item => item.Order == reg.Collection.Order))
                {
                    var msg =
                        $"{reg.ToType.FullName}: an item has already been registered with {nameof(ContainerRegistrationAttribute.Order)} " +
                        $"= {reg.Collection.Order} for collection of {reg.FromType.FullName}. A collection's " +
                        $"{nameof(ContainerRegistrationAttribute.Order)} property must be either unspecified, 0, or otherwise unique. " +
                        $"The following {nameof(ContainerRegistrationAttribute.Order)}s were previously registered: " +
                        $"{{ {string.Join(", ", registeredOrders.Select(item => item.Order))} }}.";
                    throw new RegistrationFailedException(msg);
                }
            }

            // Ensure the lifetime of the new item matches those of the existing.
            var lifetimeManager = new TLifestyleManager();
            var mismatchedItem = _collectionItemsForType[reg.FromType]
                .FirstOrDefault(item => item.LifetimeManager.GetType() != typeof(TLifestyleManager));
            if (mismatchedItem != null && mismatchedItem.LifetimeManager != default(TLifestyleManager))
            {
                var msg = $"{reg.ToType.FullName}: registered with invalid lifetime {lifetimeManager.GetType().Name}. " +
                          $"All items in a collection must be registered with the same lifetime. Lifetime is fixed once " +
                          $"the first item is registered, in this case {mismatchedItem.LifetimeManager.GetType().Name}.";
                throw new RegistrationFailedException(msg);
            }

            _collectionItemsForType[reg.FromType].Add(ResolutionItem.Create(reg.Key, reg.Collection.Order, lifetimeManager));

            // Now go wash your eyes out.
        }

        private static ContainerInterceptorWrapper CreateInterceptorWrapper()
        {
            //bool ShouldCache(RegistrationContext reg) => reg.ToType.GetMethods()
                //.Select(m => m.GetCustomAttribute<CachingAttribute>(false))
                //.Any(a => a != null);

            bool ShouldCache = false;
            bool ShouldMeasure(RegistrationContext reg) => reg.Attribute.LegacyMeasured;

            var wrapper = new ContainerInterceptorWrapper();
            //wrapper.RegisterInterceptor(CachingInterceptor.GenerateInterfaceProxy, ShouldCache);
            //wrapper.RegisterInterceptor(MeasurementInterceptor.GenerateInterfaceProxy, ShouldMeasure);
            return wrapper;
        }

        private static void EnsureNotAlreadyRegistered(RegistrationContext reg)
        {
            var type = reg.IsConcrete ? reg.ToType : reg.FromType;
            if (!_container.IsRegistered(type))
            {
                return;
            }

            var attrName = ContainerAttributeUtils.GetFriendlyName(reg.Attribute);
            var msg = $"{type}: This type has already been registered";
            if (!reg.IsConcrete)
            {
                msg += $" as {reg.ToType}";
            }
            msg += ". Registrations cannot be overriden.\n" +
                   $"To register a collection of components use to be resolved together use [{attrName}(OfCollection = true)].\n" +
                   $"To register multiple instances of the same type to be resolved individually by key use [{attrName}(Key = MyKey)]";
            throw new RegistrationFailedException(msg);
        }

        /// <summary>
        /// Ensure keys for each keyed registered type are unique
        /// </summary>
        /// <exception cref="RegistrationFailedException"></exception>
        private static void EnsureKeyUnique(RegistrationContext reg)
        {
            if (!_keysForTypes.TryGetValue(reg.FromType, out var keys))
            {
                _keysForTypes.Add(reg.FromType, new HashSet<string>(new[] { reg.Key }));
            }
            else if (keys.Contains(reg.Key))
            {
                var msg = $"{reg.ToType.FullName}: {nameof(ContainerRegistrationAttribute.Key)} \"{reg.Key}\" has " +
                          $"already been registered for {reg.FromType.FullName}. Keys must be unique.";
                throw new RegistrationFailedException(msg);
            }
        }

        public class ResolutionItem
        {
            public string Key { get; set; }
            public int Order { get; set; }
            public LifetimeManager LifetimeManager { get; set; }

            internal static ResolutionItem Create(string key, int order, LifetimeManager lifetimeManager)
                => new ResolutionItem
                {
                    Key = key,
                    Order = order,
                    LifetimeManager = lifetimeManager
                };
        }
    }
}
