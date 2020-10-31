using Agoda.IoC.Core;

namespace Agoda.IoC.ProjectUnderTest.Invalid6
{
    public interface ICollectionWithNonUniqueOrder {}
    [RegisterSingleton(OfCollection = true)] // missing Order is allowed - item position is undefined
    public class CollectionWithNonUniqueOrder0 : ICollectionWithNonUniqueOrder { }
    [RegisterSingleton(OfCollection = true, Order = 0)] // same as missing
    public class CollectionWithNonUniqueOrder01 : ICollectionWithNonUniqueOrder { }
    [RegisterSingleton(OfCollection = true, Order = 0)] // duplicate zeros are allowed
    public class CollectionWithNonUniqueOrder : ICollectionWithNonUniqueOrder { }
    [RegisterSingleton(OfCollection = true, Order = 1)]
    public class CollectionWithNonUniqueOrder1 : ICollectionWithNonUniqueOrder { }
    [RegisterSingleton(OfCollection = true, Order = 2)]
    public class CollectionWithNonUniqueOrder2 : ICollectionWithNonUniqueOrder { }
    [RegisterSingleton(OfCollection = true, Order = 3)]
    public class CollectionWithNonUniqueOrder3 : ICollectionWithNonUniqueOrder { }
    [RegisterSingleton(OfCollection = true, Order = 4)]
    public class CollectionWithNonUniqueOrder4 : ICollectionWithNonUniqueOrder { }
    [RegisterSingleton(OfCollection = true, Order = 4)] // this one fails
    public class CollectionWithNonUniqueOrder5 : ICollectionWithNonUniqueOrder { }
}
