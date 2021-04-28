using System.Collections.Generic;

namespace Agoda.IoC.Core
{
    public class KeyedComponentFactory<T> : IKeyedComponentFactory<T>
    {
        private readonly IKeyedComponentResolver<T> _componentResolver;
        private readonly HashSet<string> _registeredKeys;

        public KeyedComponentFactory(IKeyedComponentResolver<T> componentResolver)
        {
            _componentResolver = componentResolver;
            _registeredKeys = new HashSet<string>();
        }

        public T GetByKey(string key)
        {
            return _componentResolver.Resolve(key);
        }

        public bool IsRegistered(string key)
        {
            return _componentResolver.IsRegistered(key);
        }

        public T TryGetByKey(string key)
            => _componentResolver.IsRegistered(key) ? _componentResolver.Resolve(key) : default(T);

        // do not want to expose this to consumers so not part of the interface
        public void RegisterKey(string key)
        {
            _registeredKeys.Add(key);
        }
    }
}