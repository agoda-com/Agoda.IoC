using Agoda.IoC.Core;

namespace Agoda.IoC.ProjectUnderTest.Invalid8
{
    public interface IMock : IServiceWithInterfaceForMock {}
    public interface IServiceWithInterfaceForMock {}
    [RegisterSingleton(Mock = typeof(IMock))]
    public class ServiceWithInterfaceForMock : IServiceWithInterfaceForMock { }
}
