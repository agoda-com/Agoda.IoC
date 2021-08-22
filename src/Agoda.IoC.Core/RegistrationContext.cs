using System;
using System.Collections.Generic;
using System.Linq;

namespace Agoda.IoC.Core
{
    public class RegistrationContext
    {   
        public Type ToType { get; private set; }
        public Type FromType { get; private set; }
        public bool IsConcrete { get; private set; }
        public Type MockType { get; }
        public (bool IsCollection, int Order) Collection { get; }
        public string Key { get; set; }
        public Type FactoryType { get; private set; }
        public ContainerRegistrationAttribute Attribute { get; }
        public bool IsIntercepted { get; }

        public bool ReplaceServices { get; }
        public (bool IsValid, string ErrorMessage) Validation { get; }

        private readonly IList<Type> _baseTypes;
        private readonly Type _originalToType;
        private readonly Type _genericArgument;

        public RegistrationContext(
            ContainerRegistrationAttribute attribute,
            Type toType,
            bool mockMode = false,
            ContainerInterceptorWrapper wrapper = null)
        {
            Attribute = attribute;
            FromType = attribute.For;
            ToType = toType;
            IsConcrete = attribute.Concrete;
            MockType = attribute.Mock;
            FactoryType = (mockMode && MockType != null) ? null : attribute.Factory;
            Collection = (attribute.OfCollection, attribute.Order);
            Key = attribute.Key?.ToString();
            ReplaceServices = attribute.ReplaceServices;
            IsIntercepted = wrapper?.HasInterceptors(this) ?? false;
            _genericArgument = attribute.GenericArgument;            
            _baseTypes = ContainerAttributeUtils.GetBaseTypes(attribute, toType).ToList();
            
            // we might have to fiddle with the ToType to keep Unity happy, so we keep a copy of the original for error reporting
            _originalToType = ToType;

            var errorMsg = InitAndValidate().FirstOrDefault();
            Validation = errorMsg == null 
                ? (true, null) 
                : (false, errorMsg);
        }
        
        /// <summary>
        /// Validates a registration.
        /// </summary>
        /// <returns>An enumerable of error messages, or empty for a valid registration.</returns>
        private IEnumerable<string> InitAndValidate()
        {
            var actions = new Func<string>[]
            {
                EnsureNoExplicitConcreteType,
                ValidateAndDisambiguateBaseType,
                ValidateAndHandleGenerics,
                EnsureConcreteHasNoMock,
                EnsureMockCompatible,
                EnsureExplicitTypeImplemented,
                EnsureNoOverlyComplexRegistrations,
                EnsureFactoryValid,
                EnsureOrderIsUsedWithCollection,
                EnsureInterceptedImplementsInterface,
                EnsureInterceptedNotConcrete,
                EnsureInterceptedNotGeneric,
                EnsureMockIsConcrete,
                EnsureMockDoesNotDeriveFromLive,
                EnsureNotRecursive,
            };
            
            return actions
                .Select(validate => validate())
                .Where(msg => msg != null);
        }

        /// <summary>
        /// Doesn't make sense to to explicitly declare a type for a concrete registration as by definition there is
        /// only one type for consideration
        /// </summary>
        private string EnsureNoExplicitConcreteType()
        {
            if (!IsConcrete || FromType == null)
            {
                return null;
            }
            
            return
                $"{_originalToType.FullName}: cannot specify both {nameof(ContainerRegistrationAttribute.Concrete)} " +
                $"and {nameof(ContainerRegistrationAttribute.For)} parameters.";
        }

        private string ValidateAndDisambiguateBaseType()
        {
            if (FromType != null)
            {
                return null;
            }
            
            if (IsConcrete || _baseTypes.Count == 0)
            {
                IsConcrete = true;
                FromType = ToType;
                return null;    
            }

            // if there's only one base type then use it
            if (_baseTypes.Count == 1)
            {
                FromType = _baseTypes.Single();
                return null;
            }

            // if there are multiple base types, and only one is an interface, then use it
            var interfaces = _baseTypes.Where(t => t.IsInterface).ToList();
            if (interfaces.Count == 1)
            {
                FromType = interfaces.Single();
                return null;
            }

            // registration is ambiguous: multiple interfaces and/or base classes are implemented
            var attrName = ContainerAttributeUtils.GetFriendlyName(Attribute);
            return
                $"{_originalToType.FullName}: Registration is ambiguous as the implementation has multiple base types, and a " +
                $"single interface could not be determined. Please add one of the following to your registration attribute:\n" +
                string.Join("", _baseTypes.Select(t => $"\n - {nameof(ContainerRegistrationAttribute.For)} = typeof({t.Name})")) +
                $"\n\nOr, to register many base types to resolve to the same implementation, decorate the class with " +
                $"multiple registration attributes, eg:\n\n" +
                string.Join("", _baseTypes.Select(t => $"[{attrName}({nameof(ContainerRegistrationAttribute.For)} = typeof({t.Name}))]\n")) +
                $"public class {ToType.Name} ...";
        }

        private string ValidateAndHandleGenerics()
        {
            if (!FromType.IsGenericType)
            {
                return null;
            }

            if (FactoryType != null && FactoryType.IsGenericType)
            {
                if (_genericArgument == null)
                {
                    return
                        $"{_originalToType.FullName}: Factory for generic must specify the generic type, eg. " +
                        $"{nameof(ContainerRegistrationAttribute.GenericArgument)} = typeof(GenericArgumentType).";
                }

                if (_genericArgument.IsGenericType)
                {
                    return
                        $"{_originalToType.FullName}: {nameof(ContainerRegistrationAttribute.GenericArgument)} cannot " +
                        $"itself be generic.";
                }
                
                if (FromType.GenericTypeArguments.Length > 1)
                {
                    return
                        $"{_originalToType.FullName}: Factories for generics of more than 1 generic parameter are not supported.";
                }
                
                FactoryType = ContainerAttributeUtils.MakeClosedType(FactoryType, _genericArgument);
            }

            if (_genericArgument == null)
            {
                return null;
            }
            
            // if we've been given a closed generic type then convert to an open one
            if (FromType.GenericTypeArguments.Any())
            {
                FromType = FromType.GetGenericTypeDefinition();
            }
            FromType = ContainerAttributeUtils.MakeClosedType(FromType, _genericArgument);
            ToType = ContainerAttributeUtils.MakeClosedType(ToType, _genericArgument);
            
            return null;
        }

        private string EnsureOrderIsUsedWithCollection()
        {
            if (Collection.IsCollection || Collection.Order == 0)
            {
                return null;
            }

            return
                $"{_originalToType.FullName}: Cannot specify {nameof(ContainerRegistrationAttribute.Order)} unless " +
                $"{nameof(ContainerRegistrationAttribute.OfCollection)} = true.";
        }

        /// <summary>
        /// Ensures the mock type implements the same types as the live.
        /// </summary>
        private string EnsureMockCompatible()
        {
            if (IsConcrete || MockType == null || _baseTypes.Count > 1 || FromType.IsAssignableFrom(MockType))
            {
                return null;
            }

            return 
                $"{_originalToType.FullName}: The specified mock {MockType.FullName} is incompatible as it does not implement the registered type:\n" +
                FromType.FullName;
        }

        /// <summary>
        /// Validate the factory
        /// </summary>
        private string EnsureFactoryValid()
        {
            if (FactoryType == null)
            {
                return null;
            }
            
            var constructedType = IsConcrete ? ToType : FromType;

            // ensure the factory implements the correct interface to create the component
            var requiredFactoryType = typeof(IComponentFactory<>).MakeGenericType(constructedType);
            if (requiredFactoryType.IsAssignableFrom(FactoryType))
            {
                return null;
            }

            return
                $"{_originalToType.FullName}: {nameof(ContainerRegistrationAttribute.Factory)} must implement " +
                $"{requiredFactoryType.FullName}, but in fact implements {FactoryType.FullName}.";
        }

        /// <summary>
        /// We are not supporting registering using more than one of Key, Factory and OfCollection as we have not
        /// found a use case (yet) and it will probably add a lot of complexity.
        /// </summary>
        private string EnsureNoOverlyComplexRegistrations()
        {
            var count = (Key == null ? 0 : 1)
                      + (FactoryType == null ? 0 : 1)
                      + (Collection.IsCollection ? 1 : 0);
            if (count <= 1)
            {
                return null;
            }
            
            return
                $"{_originalToType.FullName}: Can specify only one of {nameof(ContainerRegistrationAttribute.Key)}, " +
                $"{nameof(ContainerRegistrationAttribute.Factory)}, or {nameof(ContainerRegistrationAttribute.OfCollection)} = true.";
        }

        /// <summary>
        /// Ensure that any explicitly registered type is indeed implemented by the class. 
        /// </summary>
        private string EnsureExplicitTypeImplemented()
        {
            if (FromType == null)
            {
                return null;
            }
            
            if (FromType.ContainsGenericParameters && IsAssignableToGenericType(ToType, FromType))
            {
                return null;
            }
            
            if (FromType.IsAssignableFrom(ToType))
            {
                return null;
            }
            
            return
                $"{_originalToType.FullName}: Explicitly registers {nameof(ContainerRegistrationAttribute.For)} " +
                $"= typeof({FromType.FullName}), but it does not implement this type.";
        }

        /// <summary>
        /// Doesn't make sense to specify a mock type for a concrete registration as by definition the type cannot change
        /// </summary>
        private string EnsureConcreteHasNoMock()
        {
            if (!IsConcrete || MockType == null)
            {
                return null;
            }
            
            return
                $"{_originalToType.FullName}: Cannot declare {nameof(ContainerRegistrationAttribute.Mock)} " +
                $"({MockType.FullName}) for concrete registration.";
        }
        
        private string EnsureInterceptedImplementsInterface()
        {
            if (IsConcrete || !IsIntercepted || FromType == null || FromType.IsInterface)
            {
                return null;
            }

            return
                $"{_originalToType.FullName}: An intercepted component must be registered for an interface. However, " +
                $"{FromType.FullName} was specified.";
        }
        
        private string EnsureInterceptedNotConcrete()
        {
            if (!IsConcrete || !IsIntercepted)
            {
                return null;
            }

            return
                $"{_originalToType.FullName}: A concrete component cannot be intercepted. It must be registered to an interface.";
        }
        
        private string EnsureInterceptedNotGeneric()
        {
            if (!IsIntercepted || FromType == null || !FromType.ContainsGenericParameters)
            {
                return null;
            }

            return $"{_originalToType.FullName}: Intercepted generic types are not (yet) supported by attribute registration.";
        }
        
        private string EnsureMockIsConcrete()
        {
            if (MockType == null || !MockType.IsAbstract)
            {
                return null;
            }
            
            return $"{_originalToType.FullName}: Mock must be a concrete type. An abstract class or interface was specified: {MockType.FullName}";
        }
        
        private string EnsureMockDoesNotDeriveFromLive()
        {
            if (MockType == null || !ToType.IsAssignableFrom(MockType))
            {
                return null;
            }
            
            return
                $"{_originalToType.FullName}: Mock should not derive from the live implementation. Instead, it should implement " +
                $"the same interface. To share functionality between the mock and the live, create an abstract base class.";
        }
        
        private string EnsureNotRecursive()
        {
            if (IsConcrete || FromType != ToType)
            {
                return null;
            }

            return
                $"{_originalToType.FullName}: Registration is recursive. To register a concrete type use " +
                $"{nameof(ContainerRegistrationAttribute.Concrete)} = true. Or did you mean to specify an interface?";
        }
        
        // https://stackoverflow.com/questions/5461295/using-isassignablefrom-with-open-generic-types
        private static bool IsAssignableToGenericType(Type givenType, Type genericType)
        {
            var interfaceTypes = givenType.GetInterfaces();

            if (interfaceTypes.Any(it => it.IsGenericType && it.GetGenericTypeDefinition() == genericType))
            {
                return true;
            }

            if (givenType.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
                return true;

            var baseType = givenType.BaseType;
            return baseType != null && IsAssignableToGenericType(baseType, genericType);
        }
    }
}
