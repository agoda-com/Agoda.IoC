using Agoda.IoC.Core;

namespace Agoda.IoC.ProjectUnderTest.Invalid3
{
    public interface IIncompatibleMock {}
    public class RandomType {}
    
    [RegisterSingleton(Mock = typeof(RandomType))]
    public class IncompatibleMock : IIncompatibleMock {}
    
    
}
