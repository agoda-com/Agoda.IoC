using Agoda.IoC.Core;

namespace Agoda.IoC.ProjectUnderTest.Invalid11
{
    public interface IRecursive {}
    [RegisterSingleton(For = typeof(Recursive))]
    public class Recursive : IRecursive
    {
    }
}
