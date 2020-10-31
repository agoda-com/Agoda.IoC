using Agoda.IoC.Core;

namespace Agoda.IoC.ProjectUnderTest.Invalid1
{
    // concrete implementations cannot specify a mock
    
    [RegisterSingleton(Mock = typeof(ThisShouldntWork))]
    public class ConcreteImplementationWithMock
    {
    }

    public class ThisShouldntWork : ConcreteImplementationWithMock
    {
    }
}
