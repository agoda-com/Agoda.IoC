using Agoda.IoC.Generator.UnitTests.Helpers;

namespace Agoda.IoC.Generator.UnitTests;

[TestFixture]
public class ContainerRegistrationGeneratorTransientUnitTests
{
    private static IEnumerable<TestCaseData> ContainerRegistrationGeneratorTestDatas()
    {
        yield return new TestCaseData(@"
using using Agoda.IoC.Generator.Abstractions;
namespace Agoda.IoC.Generator.UnitTests;
[RegisterTransient]
public class ClassA{
}
", @"serviceCollection.AddTransient<ClassA>();
return serviceCollection;");
        yield return new TestCaseData(@"
using using Agoda.IoC.Generator.Abstractions;
namespace Agoda.IoC.Generator.UnitTests;
[RegisterTransient]
public class ClassA{
}
", @"serviceCollection.AddTransient<ClassA>();
return serviceCollection;");
        yield return new TestCaseData(@"
using using Agoda.IoC.Generator.Abstractions;
namespace Agoda.IoC.Generator.UnitTests;
[RegisterTransient(ReplaceService = true)]
public class ClassA{
}
", @"serviceCollection.Replace(new ServiceDescriptor(typeof(ClassA), ServiceLifetime.Transient));
return serviceCollection;");
        yield return new TestCaseData(@"
using using Agoda.IoC.Generator.Abstractions;
namespace Agoda.IoC.Generator.UnitTests;
[RegisterTransient(ReplaceService = true, For = typeof(IClassA))]
public class ClassA : IClassA{
}
public interface IClassA{
}
", @"serviceCollection.Replace(new ServiceDescriptor(typeof(IClassA), typeof(ClassA), ServiceLifetime.Transient));
return serviceCollection;");
        yield return new TestCaseData(@"
using using Agoda.IoC.Generator.Abstractions;
namespace Agoda.IoC.Generator.UnitTests;
[RegisterTransient(Concrete = true)]
public class ClassA{
}
", @"serviceCollection.AddTransient<ClassA>();
return serviceCollection;");
        yield return new TestCaseData(@"
using using Agoda.IoC.Generator.Abstractions;
namespace Agoda.IoC.Generator.UnitTests;
    [RegisterTransient(For = typeof(IPartialClassTest2))]
    public class PartialClassTest2 : IPartialClassTest2
    {
    }

    public interface IPartialClassTest2
    {
    }
", @"serviceCollection.AddTransient<IPartialClassTest2, PartialClassTest2>();
return serviceCollection;");
        yield return new TestCaseData(@"
using using Agoda.IoC.Generator.Abstractions;
namespace Agoda.IoC.Generator.UnitTests;
[RegisterTransient( Factory = typeof(ClassBImplementationFactory))]
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
", @"serviceCollection.AddTransient(sp => new ClassBImplementationFactory().Factory(sp));
return serviceCollection;");
        yield return new TestCaseData(@"
using using Agoda.IoC.Generator.Abstractions;
namespace Agoda.IoC.Generator.UnitTests;
[RegisterTransient( Factory = typeof(ClassBImplementationFactory))]
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
", @"serviceCollection.AddTransient(sp => new ClassBImplementationFactory().Factory(sp));
return serviceCollection;");
        yield return new TestCaseData(@"
using using Agoda.IoC.Generator.Abstractions;
namespace Agoda.IoC.Generator.UnitTests;
    public interface IThing<T,U>
    {
        string GetNameT { get; }
        string GetNameU { get; }
    }
    [RegisterTransient(For = typeof(IThing<,>))]
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
", @"serviceCollection.AddTransient(typeof(IThing<, >), typeof(GenericThing<, >));
return serviceCollection;");
    }

    [Test, TestCaseSource("ContainerRegistrationGeneratorTestDatas")]
    public void Should_Generate_AddTransient_Correctly(string source, string generatedBodyMethod)
    {
        TestHelper.GenerateAgodaIoC(source)
                .Should()
                .HaveMethodCount(2)
                .HaveMethods("Register", "RegisterFromAgodaIoCGeneratorUnitTests")
                .HaveMethodBody("Register", generatedBodyMethod);
    }
}
