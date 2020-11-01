using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Agoda.IoC.Core
{
    /// <summary>
    /// Non-vendor specific utility functions for attribute registration.
    /// </summary>
    public static class ContainerAttributeUtils
    { 
        private static readonly string[] ExcludedAssemblyNamePrefixes = { "mscorlib", "System" };

        public static IEnumerable<Type> GetBaseTypes(ContainerRegistrationAttribute attribute, Type implementation)
        {   
            if (attribute.Concrete)
            {
                return Enumerable.Empty<Type>();
            }

            return attribute.For != null 
                ? new[] {attribute.For} 
                : GetBaseTypes(implementation);
        }

        /// <summary>
        /// We never want to consider base types from the BCL, eg. IDisposable, ICloneable, NameValueCollection.
        /// Registered types may well derive from these (and of course from Object), so we should just silently
        /// skip them when trying to find a suitable base type from which to register.
        /// </summary> 
        private static bool IsRationalForRegistration(Type type)
        {
            return ExcludedAssemblyNamePrefixes.All(prefix => !type.Assembly.FullName.StartsWith(prefix));
        }

        private static IEnumerable<Type> GetBaseTypes(Type type)
        {
            foreach (var i in type.GetInterfaces().Where(IsRationalForRegistration))
            {
                yield return i;
            }

            var baseType = type.BaseType;
            while (baseType != null && IsRationalForRegistration(baseType))
            {
                yield return baseType;
                baseType = baseType.BaseType;
            }
        }
        
        public static void ThrowIfError(IList<string> errorMsgs)
        {
            if (!errorMsgs.Any())
            {
                return;
            }
            
            if (errorMsgs.Count > 1)
            {
                errorMsgs.Insert(0, $"There were {errorMsgs.Count} errors encountered during component registration.");
            }

            var msg = string.Join("\n\n--------------------------------------------------------------\n\n", errorMsgs);
            throw new RegistrationFailedException(msg + "\n");
        }

        public static string GetFriendlyName(Attribute attr)
        {
            var attrName = attr.GetType().Name;
            return Regex.Replace(attrName, "Attribute$", "");
        }
        
        /// <summary>
        /// Creates a closed type from either an open generic type definition (Type&lt;&gt;), a closed generic type
        /// (Type&lt;T&gt;), or returns the original type if it is not generic.
        /// </summary>
        /// <remarks>
        /// Works only for generic types of 1 argument, which is currently enough for our use-case.
        /// </remarks>
        /// <param name="definition">A closed or open generic type, or a non-generic type.</param>
        /// <param name="parameter">The type argument to inject for the generic type</param>
        public static Type MakeClosedType(Type definition, Type parameter)
        {
            // non-generic
            if (!definition.IsGenericTypeDefinition && !definition.IsGenericType)
            {
                return definition;
            }

            // closed generic
            if (definition.ContainsGenericParameters)
            {
                return definition.MakeGenericType(parameter);
            }
            
            // open generic
            var definitionStack = new Stack<Type>();
            var type = definition;
            while (!type.IsGenericTypeDefinition)
            {
                definitionStack.Push(type.GetGenericTypeDefinition());
                type = type.GetGenericArguments()[0];
            }
            type = type.MakeGenericType(parameter);
            while (definitionStack.Count > 0)
            {
                type = definitionStack.Pop().MakeGenericType(type);
            }

            return type;
        }
    }
}
