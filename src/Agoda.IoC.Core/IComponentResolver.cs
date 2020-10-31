namespace Agoda.IoC.Core
{
    public interface IComponentResolver
    {
        T Resolve<T>();
    }
}