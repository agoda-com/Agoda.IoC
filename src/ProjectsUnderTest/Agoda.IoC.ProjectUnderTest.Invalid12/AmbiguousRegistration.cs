using Agoda.IoC.Core;

namespace Agoda.IoC.ProjectUnderTest.Invalid12
{
    public interface IMyBaseInterface {}
    public interface IMyBaseInterface2 : IMyBaseInterface {}
    public class MyAmbiguousBaseClass : IMyBaseInterface2 {}
    [RegisterSingleton]
    public class AmbiguousRegistration : MyAmbiguousBaseClass {}
}
