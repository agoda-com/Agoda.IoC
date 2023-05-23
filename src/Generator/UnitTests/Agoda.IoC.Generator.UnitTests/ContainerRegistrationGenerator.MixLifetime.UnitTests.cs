using Agoda.IoC.Generator.UnitTests.Helpers;

namespace Agoda.IoC.Generator.UnitTests;

[TestFixture]
public class ContainerRegistrationGeneratorMixLifetimeTests
{
    private static IEnumerable<TestCaseData> ContainerRegistrationGeneratorTestDatas()
    {
        yield return new TestCaseData(@"
using using Agoda.IoC.Generator.Abstractions;
namespace Agoda.IoC.Generator.UnitTests;

[RegisterScoped]
public class ClassA{
}
[RegisterScoped(ReplaceService = true)]
public class ClassB{
}


", @"serviceCollection.AddScoped<ClassA>();
serviceCollection.Replace(new ServiceDescriptor(typeof(ClassB), ServiceLifetime.Scoped));
return serviceCollection;");
        yield return new TestCaseData(@"
using using Agoda.IoC.Generator.Abstractions;
namespace Agoda.IoC.Generator.UnitTests;
[RegisterScoped(Factory = typeof(ClassBImplementationFactory))]
public class ClassB : IClassB { }
public interface IClassB { }


public class ClassBImplementationFactory : IImplementationFactory<ClassB>
{
    public ClassB Factory(IServiceProvider serviceProvider)
    {
        return new ClassB();
    }
}

[RegisterSingleton(Factory = typeof(ClassCImplementationFactory))]
public class ClassC : IClassC { }
public interface IClassC { }
public class ClassCImplementationFactory : IImplementationFactory<ClassC>
{
    public ClassC Factory(IServiceProvider serviceProvider)
    {
        return new ClassC();
    }
}
[RegisterTransient(ReplaceService =true)]
public class ReplaceA
{
}

public interface IThing<T, U>
{
    string GetNameT { get; }
    string GetNameU { get; }
}
[RegisterScoped(For = typeof(IThing<,>))]
public class GenericThing<T, U> : IThing<T, U>
{
    public GenericThing()
    {
        GetNameT = typeof(T).Name;
        GetNameU = typeof(U).Name;
    }
    public string GetNameT { get; }
    public string GetNameU { get; }
}
",
@"serviceCollection.AddScoped(sp => new ClassBImplementationFactory().Factory(sp));
serviceCollection.AddSingleton(sp => new ClassCImplementationFactory().Factory(sp));
serviceCollection.Replace(new ServiceDescriptor(typeof(ReplaceA), ServiceLifetime.Transient));
serviceCollection.AddScoped(typeof(IThing<, >), typeof(GenericThing<, >));
return serviceCollection;");
    }
    [Test, TestCaseSource("ContainerRegistrationGeneratorTestDatas")]
    public void Should_Generate_AddScoped_Correctly(string source, string generatedBodyMethod)
    {
        TestHelper.GenerateAgodaIoC(source)
                .Should()
                .HaveMethodCount(2)
                .HaveMethods("Register", "RegisterFromAgodaIoCGeneratorUnitTests")
                .HaveMethodBody("Register", generatedBodyMethod);
    }

    [TestCase(
        @"using using Agoda.IoC.Generator.Abstractions;
namespace Agoda.IoC.Generator.UnitTests;

[RegisterScoped]
public class ClassA: IClassA{
}
public interface IClassA{
}

[RegisterSingleton]
public class DoWork<T> where T : new()
{
    public T Process()
    {
        return new T();
    }
}
",
        @"serviceCollection.AddScoped<IClassA, ClassA>();
serviceCollection.AddSingleton(typeof(DoWork<>));
return serviceCollection;
")]

    public void Should_Generate_With_TypeOf_Correctly(string source, string generatedBodyMethod)
    {
        TestHelper.GenerateAgodaIoC(source)
                .Should()
                .HaveMethodCount(2)
                .HaveMethods("Register", "RegisterFromAgodaIoCGeneratorUnitTests")
                .HaveMethodBody("Register", generatedBodyMethod);
    }

    [TestCase(
    @"using using Agoda.IoC.Generator.Abstractions;
namespace Agoda.IoC.Generator.UnitTests;

[RegisterSingleton]
public class GenericDoWork<T> : IDoWork<T> where T : new ()
{

    public T Process()
    {
        return new T();
    }
}
public interface IDoWork<T> where T : new()
{
    T Process();
}
",
    @"serviceCollection.AddSingleton(typeof(IDoWork<>), typeof(GenericDoWork<>));
return serviceCollection;
")]

    public void Should_Generate_With_First_interface_Correctly(string source, string generatedBodyMethod)
    {
        TestHelper.GenerateAgodaIoC(source)
                .Should()
                .HaveMethodCount(2)
                .HaveMethods("Register", "RegisterFromAgodaIoCGeneratorUnitTests")
                .HaveMethodBody("Register", generatedBodyMethod);
    }

}