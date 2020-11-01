using Agoda.IoC.Core;

namespace Agoda.IoC.ProjectUnderTest.Invalid4
{
    public interface IOrderWithoutCollection {}
    [RegisterSingleton(Order = 1)] 
    public class OrderWithoutCollection : IOrderWithoutCollection
    {
    }
}
