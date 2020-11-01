using Agoda.IoC.Core;
using Microsoft.Practices.Unity;


namespace Agoda.IoC.Unity
{
    public class UnityComponentResolver : IComponentResolver
    {
        private readonly IUnityContainer _container;

        public UnityComponentResolver(IUnityContainer container)
        {
            _container = container;
        }

        public T Resolve<T>()
        {
            return _container.Resolve<T>();
        }
    }
}
