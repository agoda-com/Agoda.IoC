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

        // OfCollection case
        yield return new TestCaseData(@"
using using Agoda.IoC.Generator.Abstractions;
namespace Agoda.IoC.Generator.UnitTests;
[RegisterTransient(For = typeof(IPipeline), OfCollection = true, Order = 2)]
public class Pipeline2 : IPipeline
{
    public string Invoke()
    {
        return nameof(Pipeline2);
    }
}
[RegisterTransient(For = typeof(IPipeline), OfCollection = true, Order = 3)]
public class Pipeline3 : IPipeline
{
    public string Invoke()
    {
        return nameof(Pipeline2);
    }
}
[RegisterTransient(For = typeof(IPipeline), OfCollection = true, Order = 1)]
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
[RegisterTransient(For = typeof(IMiddleware), OfCollection = true, Order = 3)]
public class IMiddleware2 : IMiddleware
{
    public string Invoke()
    {
        return nameof(IMiddleware1);
    }
}
[RegisterTransient(For = typeof(IMiddleware), OfCollection = true, Order = 1)]
public class IMiddleware1 : IMiddleware
{
    public string Invoke()
    {
        return nameof(IMiddleware1);
    }
}
public interface IMiddleware
{
    string Invoke();
}

",
@"// Of Collection code
serviceCollection.AddTransient<IMiddleware, IMiddleware1>();
serviceCollection.AddTransient<IMiddleware, IMiddleware2>();
serviceCollection.AddTransient<IPipeline, Pipeline1>();
serviceCollection.AddTransient<IPipeline, Pipeline2>();
serviceCollection.AddTransient<IPipeline, Pipeline3>();
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
