using Agoda.IoC.Generator.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Agoda.IoC.Generator.IntegrationTest1;

public class UnitTest1
{
    [Fact]
    public void ServiceCollectionTest()
    {
        var services = new ServiceCollection();
        services.RegisterFromAgodaIoCGeneratorIntegrationTest1();
        Assert.Contains(services, x => x.Lifetime == ServiceLifetime.Scoped && x.ImplementationType == typeof(ClassA));
         }
}


[RegisterScoped(Concrete = true)]
public class ClassA : IClassA
{
}
public interface IClassA { }


[RegisterSingleton(Factory = typeof(ClassBImplementationFactory))]
public class ClassB : IClassB
{
}
public interface IClassB { }


public class ClassBImplementationFactory : IImplementationFactory<IClassB>
{
    public IClassB Factory(IServiceProvider serviceProvider)
    {
        return new ClassB();
    }
}
