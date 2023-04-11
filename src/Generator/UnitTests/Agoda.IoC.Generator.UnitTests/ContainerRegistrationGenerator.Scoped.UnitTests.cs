using Agoda.IoC.Generator.UnitTests.Helpers;
using System.Collections;

namespace Agoda.IoC.Generator.UnitTests;

public class ContainerRegistrationGeneratorScopedUnitTests
{
    [Theory, ClassData(typeof(ScopedTestDatas))]
    public void Should_Generate_AddScoped_Correctly(string source, string generatedBodyMethod)
    {
        TestHelper.GenerateAgodaIoC(source)
                .Should()
                .HaveMethodCount(2)
                .HaveMethods("Register", "RegisterFromAgodaIoCGeneratorUnitTests")
                .HaveMethodBody("Register", generatedBodyMethod);
    }
}

internal class ScopedTestDatas : IEnumerable<object[]>
{
    private readonly List<object[]> _data = new List<object[]>
    {
        //Should_Generate_AddScoped_With_Concrete_Type
        new object[] { @"
using using Agoda.IoC.Generator.Abstractions;
namespace Agoda.IoC.Generator.UnitTests;
[RegisterScoped]
public class ClassA{
}
", @"serviceCollection.AddScoped<ClassA>();
return serviceCollection;" },
        // Should_Generate_AddScoped_Concrete_With_Concrete_Type
        new object[] { @"
using using Agoda.IoC.Generator.Abstractions;
namespace Agoda.IoC.Generator.UnitTests;
[RegisterScoped(Concrete = true)]
public class ClassA{
}
", @"serviceCollection.AddScoped<ClassA>();
return serviceCollection;"},
        //Should_Generate_AddScoped_With_Interface
        new object[] { @"
using using Agoda.IoC.Generator.Abstractions;
namespace Agoda.IoC.Generator.UnitTests;
    [RegisterScoped(For = typeof(IPartialClassTest2))]
    public class PartialClassTest2 : IPartialClassTest2
    {
    }

    public interface IPartialClassTest2
    {
    }
", @"serviceCollection.AddScoped<IPartialClassTest2, PartialClassTest2>();
return serviceCollection;"},
        //Should_Generate_AddScoped_Factory_With_Interface
        new object[] { @"
using using Agoda.IoC.Generator.Abstractions;
namespace Agoda.IoC.Generator.UnitTests;
[RegisterScoped( Factory = typeof(ClassBImplementationFactory))]
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
", @"serviceCollection.AddScoped(sp => new ClassBImplementationFactory().Factory(sp));
return serviceCollection;"},
        //Should_Generate_AddScoped_Factory_With_Concrete
        new object[] { @"
using using Agoda.IoC.Generator.Abstractions;
namespace Agoda.IoC.Generator.UnitTests;
[RegisterScoped( Factory = typeof(ClassBImplementationFactory))]
public class ClassB : IClassB
{
}
public interface IClassB { }


public class ClassBImplementationFactory : IImplementationFactory<ClassB>
{
    public IClassB Factory(IServiceProvider serviceProvider)
    {
        return new ClassB();
    }
}
", @"serviceCollection.AddScoped(sp => new ClassBImplementationFactory().Factory(sp));
return serviceCollection;"},
        //Should_Generate_AddScoped_With_Open_Generic
         new object[] { @"
using using Agoda.IoC.Generator.Abstractions;
namespace Agoda.IoC.Generator.UnitTests;
    public interface IThing<T,U>
    {
        string GetNameT { get; }
        string GetNameU { get; }
    }
    [RegisterScoped(For = typeof(IThing<,>))]
    public class GenericThing<T,U> : IThing<T,U>
    {
        public GenericThing()
        {
            GetNameT = typeof(T).Name;
            GetNameU = typeof(U).Name;
        }
        public string GetNameT { get; }
        public string GetNameU { get; }
    }
",@"serviceCollection.AddScoped(typeof(IThing<, >), typeof(GenericThing<, >));
return serviceCollection;"},
    };

    public IEnumerator<object[]> GetEnumerator()
    {
        return _data.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        { return GetEnumerator(); }
    }
}