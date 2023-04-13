using Agoda.IoC.Generator.UnitTests.Helpers;

namespace Agoda.IoC.Generator.UnitTests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ContainerRegistrationGeneratorSingletonUnitTests
{
    private static IEnumerable<TestCaseData> ContainerRegistrationGeneratorTestDatas()
    {
        yield return new TestCaseData(@"
using using Agoda.IoC.Generator.Abstractions;
namespace Agoda.IoC.Generator.UnitTests;
[RegisterSingleton]
public class ClassA{
}
", @"serviceCollection.AddSingleton<ClassA>();
return serviceCollection;");
        yield return new TestCaseData(@"
using using Agoda.IoC.Generator.Abstractions;
namespace Agoda.IoC.Generator.UnitTests;
[RegisterSingleton(Concrete = true)]
public class ClassA{
}
", @"serviceCollection.AddSingleton<ClassA>();
return serviceCollection;");
        yield return new TestCaseData(@"
using using Agoda.IoC.Generator.Abstractions;
namespace Agoda.IoC.Generator.UnitTests;
    [RegisterSingleton(For = typeof(IPartialClassTest2))]
    public class PartialClassTest2 : IPartialClassTest2
    {
    }

    public interface IPartialClassTest2
    {
    }
", @"serviceCollection.AddSingleton<IPartialClassTest2, PartialClassTest2>();
return serviceCollection;");
        yield return new TestCaseData(@"
using using Agoda.IoC.Generator.Abstractions;
namespace Agoda.IoC.Generator.UnitTests;
[RegisterSingleton( Factory = typeof(ClassBImplementationFactory))]
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
", @"serviceCollection.AddSingleton(sp => new ClassBImplementationFactory().Factory(sp));
return serviceCollection;");
        yield return new TestCaseData(@"
using using Agoda.IoC.Generator.Abstractions;
namespace Agoda.IoC.Generator.UnitTests;
[RegisterSingleton( Factory = typeof(ClassBImplementationFactory))]
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
", @"serviceCollection.AddSingleton(sp => new ClassBImplementationFactory().Factory(sp));
return serviceCollection;");
        yield return new TestCaseData(@"
using using Agoda.IoC.Generator.Abstractions;
namespace Agoda.IoC.Generator.UnitTests;
    public interface IThing<T,U>
    {
        string GetNameT { get; }
        string GetNameU { get; }
    }
    [RegisterSingleton(For = typeof(IThing<,>))]
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
", @"serviceCollection.AddSingleton(typeof(IThing<, >), typeof(GenericThing<, >));
return serviceCollection;");
    }
    [Test, TestCaseSource("ContainerRegistrationGeneratorTestDatas")]
    public void Should_Generate_AddSingleton_Correctly(string source, string generatedBodyMethod)
    {
        TestHelper.GenerateAgodaIoC(source)
                .Should()
                .HaveMethodCount(2)
                .HaveMethods("Register", "RegisterFromAgodaIoCGeneratorUnitTests")
                .HaveMethodBody("Register", generatedBodyMethod);
    }
}
