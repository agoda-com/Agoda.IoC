using Agoda.IoC.Generator.Abstractions;

namespace GeneratorExample;

[RegisterScoped(Concrete = true)]
public class ClassA : IClassA
{
}
public interface IClassA { }


[RegisterSingleton(Factory = typeof(ClassBImplementationFactory))]
public class ClassC : IClassC
{
}
public interface IClassC { }


public class ClassBImplementationFactory : IImplementationFactory<IClassC>
{
    public IClassC Factory(IServiceProvider serviceProvider)
    {
        return new ClassC();
    }
}

[RegisterSingleton(For = typeof(IPipeline), OfCollection = true, Order = 3)]
public class Pipeline3 : IPipeline
{
    public string Invoke()
    {
        return nameof(Pipeline2);
    }
}




[RegisterSingleton(For = typeof(IPipeline), OfCollection = true, Order = 2)]
public class Pipeline2 : IPipeline
{
    public string Invoke()
    {
        return nameof(Pipeline2);
    }
}

[RegisterSingleton(For = typeof(IPipeline), OfCollection = true, Order = 1)]
public class Pipeline1 : IPipeline
{
    public string Invoke()
    {
        return nameof(Pipeline1);
    }
}



public interface IPipeline
{
    string Invoke();
}