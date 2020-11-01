namespace Agoda.IoC.Core
{
    public interface IKeyedComponentFactory<out T>
    {
        T GetByKey(object key);
        bool IsRegistered(object key);
        T TryGetByKey(object key);
    }
}