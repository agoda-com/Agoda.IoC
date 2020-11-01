namespace Agoda.IoC.Core
{
    public interface IComponentFactory<out T>
    {
        T Build(IComponentResolver c);
    }
}
