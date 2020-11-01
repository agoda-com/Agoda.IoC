using Agoda.IoC.Core;

namespace Agoda.IoC.ProjectUnderTest.Invalid15
{
    public interface IAmbiguousIntercepted {}
    public interface IAmbiguousIntercepted2 {}
    [RegisterSingleton(LegacyMeasured = true)]
    public class AmbiguousIntercepted : IAmbiguousIntercepted, IAmbiguousIntercepted2
    {
    }

    
}
