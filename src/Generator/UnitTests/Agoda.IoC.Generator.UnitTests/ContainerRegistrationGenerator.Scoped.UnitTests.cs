using Agoda.IoC.Generator.UnitTests.Helpers;
using System.Collections;

namespace Agoda.IoC.Generator.UnitTests;

[TestFixture]
public class ContainerRegistrationGeneratorScopedUnitTests
{
    private static IEnumerable<TestCaseData> ContainerRegistrationGeneratorTestDatas()
    {
        yield return new TestCaseData( @"
using using Agoda.IoC.Generator.Abstractions;
namespace Agoda.IoC.Generator.UnitTests;
[RegisterScoped]
public class ClassA{
}
", @"serviceCollection.AddScoped<ClassA>();
return serviceCollection;" );


        yield return new TestCaseData(@"
using using Agoda.IoC.Generator.Abstractions;
namespace Agoda.IoC.Generator.UnitTests;
[RegisterScoped(Concrete = true)]
[RegisterScoped(Concrete = false)]
public class ClassA : IClassA{
}
public interface IClassA {}
", @"serviceCollection.AddScoped<ClassA>();
serviceCollection.AddScoped<IClassA, ClassA>();
return serviceCollection;");

        yield return new TestCaseData(@"
using using Agoda.IoC.Generator.Abstractions;
namespace Agoda.IoC.Generator.UnitTests;
[RegisterPerRequest]
public class ClassA{
}
", @"serviceCollection.AddScoped<ClassA>();
return serviceCollection;");

        yield return new TestCaseData(@"
using using Agoda.IoC.Generator.Abstractions;
namespace Agoda.IoC.Generator.UnitTests;
[RegisterScoped(Concrete = true)]
public class ClassA{
}
", @"serviceCollection.AddScoped<ClassA>();
return serviceCollection;");

        yield return new TestCaseData(@"
using using Agoda.IoC.Generator.Abstractions;
namespace Agoda.IoC.Generator.UnitTests;
[RegisterPerRequest(Concrete = true)]
public class ClassA{
}
", @"serviceCollection.AddScoped<ClassA>();
return serviceCollection;");

        yield return new TestCaseData(@"
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
return serviceCollection;");

        yield return new TestCaseData(@"
using using Agoda.IoC.Generator.Abstractions;
namespace Agoda.IoC.Generator.UnitTests;
    [RegisterPerRequest(For = typeof(IPartialClassTest2))]
    public class PartialClassTest2 : IPartialClassTest2
    {
    }

    public interface IPartialClassTest2
    {
    }
", @"serviceCollection.AddScoped<IPartialClassTest2, PartialClassTest2>();
return serviceCollection;");



        yield return new TestCaseData(@"
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
return serviceCollection;");
        yield return new TestCaseData(@"
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
return serviceCollection;");
        yield return new TestCaseData(@"
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
", @"serviceCollection.AddScoped(typeof(IThing<, >), typeof(GenericThing<, >));
return serviceCollection;");

        // OfCollection case
        yield return new TestCaseData(@"
using using Agoda.IoC.Generator.Abstractions;
namespace Agoda.IoC.Generator.UnitTests;
[RegisterScoped(For = typeof(IPipeline), OfCollection = true, Order = 2)]
public class Pipeline2 : IPipeline
{
    public string Invoke()
    {
        return nameof(Pipeline2);
    }
}
[RegisterScoped(For = typeof(IPipeline), OfCollection = true, Order = 3)]
public class Pipeline3 : IPipeline
{
    public string Invoke()
    {
        return nameof(Pipeline2);
    }
}
[RegisterScoped(For = typeof(IPipeline), OfCollection = true, Order = 1)]
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
[RegisterScoped(For = typeof(IMiddleware), OfCollection = true, Order = 3)]
public class IMiddleware2 : IMiddleware
{
    public string Invoke()
    {
        return nameof(IMiddleware1);
    }
}
[RegisterPerRequest(For = typeof(IMiddleware), OfCollection = true, Order = 1)]
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
serviceCollection.AddScoped<IMiddleware, IMiddleware1>();
serviceCollection.AddScoped<IMiddleware, IMiddleware2>();
serviceCollection.AddScoped<IPipeline, Pipeline1>();
serviceCollection.AddScoped<IPipeline, Pipeline2>();
serviceCollection.AddScoped<IPipeline, Pipeline3>();
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
}