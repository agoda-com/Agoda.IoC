using System.Web.Http.Dependencies;
using Microsoft.Practices.Unity;

namespace Agoda.IoC.Unity
{
    public class UnityHttpDependencyResolver : UnityDependencyScope, IDependencyResolver
    {
        public UnityHttpDependencyResolver(IUnityContainer container)
            : base(container)
        {
        }

        public IDependencyScope BeginScope()
        {
            var childContainer = Container.CreateChildContainer();

            return new UnityDependencyScope(childContainer);
        }
    }
}
