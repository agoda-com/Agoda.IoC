namespace Agoda.IoC.Core
{
    public interface IKeyedComponentResolver<out T>
    {
        T Resolve(string key);
        bool IsRegistered(string key);
    }
}