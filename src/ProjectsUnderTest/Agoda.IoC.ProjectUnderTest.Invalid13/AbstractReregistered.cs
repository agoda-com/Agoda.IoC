using Agoda.IoC.Core;

namespace Agoda.IoC.ProjectUnderTest.Invalid13
{
    public interface IAbstractReregistered {}
    [RegisterSingleton]
    public class Abstractegistered : IAbstractReregistered {}
    [RegisterSingleton]
    public class AbstractReregistered : IAbstractReregistered {}
    
}
