using Agoda.IoC.Core;
using Microsoft.Practices.Unity;

namespace Agoda.IoC.Unity
{
    public class UnityKeyedComponentResolver<T> : IKeyedComponentResolver<T>
    {
        private readonly IUnityContainer _container;

        public UnityKeyedComponentResolver(IUnityContainer container)
        {
            _container = container;
        }

        public T Resolve(object key)
        {
            return _container.Resolve<T>(key.ToString());
        }

        public bool IsRegistered(object key)
        {
            return _container.IsRegistered<T>(key.ToString());
        }
    }
}
