using Agoda.IoC.Core;

namespace Agoda.IoC.ProjectUnderTest.Invalid5
{
    public interface IMismatchedCollectionLifetime {}
    
    [RegisterSingleton(OfCollection = true)]
    public class MismatchedCollectionLifetime1 : IMismatchedCollectionLifetime {}
    
    [RegisterTransient(OfCollection = true)]
    public class MismatchedCollectionLifetime2 : IMismatchedCollectionLifetime {}
}
