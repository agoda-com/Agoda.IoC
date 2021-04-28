namespace Agoda.IoC.Core
{
    public interface IKeyedComponentFactory<out T>
    {
        T GetByKey(string key);
        bool IsRegistered(string key);
        T TryGetByKey(string key);
    }
}