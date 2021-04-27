using System.Collections.Generic;

namespace Agoda.IoC.Core
{
    public class KeyedComponentFactory<T> : IKeyedComponentFactory<T>
    {
        private readonly IKeyedComponentResolver<T> _componentResolver;
        private readonly HashSet<object> _registeredKeys;

        public KeyedComponentFactory(IKeyedComponentResolver<T> componentResolver)
        {
            _componentResolver = componentResolver;
            _registeredKeys = new HashSet<object>();
        }

        public T GetByKey(object key)
        {
            return _componentResolver.Resolve(key);
        }

        public bool IsRegistered(object key)
        {
            return _componentResolver.IsRegistered(key);
        }

        public T TryGetByKey(object key)
            => _componentResolver.IsRegistered(key) ? _componentResolver.Resolve(key) : default(T);

        // do not want to expose this to consumers so not part of the interface
        public void RegisterKey(object key)
        {
            _registeredKeys.Add(key);
        }
    }
}