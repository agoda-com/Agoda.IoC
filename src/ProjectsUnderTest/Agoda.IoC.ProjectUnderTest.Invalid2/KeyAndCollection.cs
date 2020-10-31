using Agoda.IoC.Core;

namespace Agoda.IoC.ProjectUnderTest.Invalid2
{
    // Invalid registration with factory key and ofCollection
    public interface IHybridOfCollectionFactoryService {}
    [RegisterPerRequest(Key = "HybridFactoryService", OfCollection = true)]
    public class HybridFactoryService : IHybridOfCollectionFactoryService {}
}
