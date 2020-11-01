namespace Agoda.IoC.Core
{
    public interface IKeyedComponentResolver<out T>
    {
        T Resolve(object key);
        bool IsRegistered(object key);
    }
}