using Agoda.IoC.Core;

namespace Agoda.IoC.ProjectUnderTest.Invalid7
{
    public interface IMockThatDerivesFromLive {}
    public class MockService : LiveService {}
    [RegisterSingleton(Mock = typeof(MockService))]
    public class LiveService : IMockThatDerivesFromLive
    {
    }
}
