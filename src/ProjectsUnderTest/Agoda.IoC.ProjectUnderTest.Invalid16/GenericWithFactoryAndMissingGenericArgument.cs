using System;
using Agoda.IoC.Core;

namespace Agoda.IoC.ProjectUnderTest.Invalid16
{
    public class GenericWithFactoryAndMissingGenericArgumentFactory<T1> : IComponentFactory<IGenericWithFactoryAndMissingGenericArgument<T1>>
    {
        public IGenericWithFactoryAndMissingGenericArgument<T1> Build(IComponentResolver c)
        {
            throw new NotImplementedException();
        }
    }
    
    public interface IGenericWithFactoryAndMissingGenericArgument<in T> {}

    [RegisterTransient(Factory = typeof(GenericWithFactoryAndMissingGenericArgumentFactory<>))]
    public class GenericWithFactoryAndMissingGenericArgument<T1> : IGenericWithFactoryAndMissingGenericArgument<T1> {}
}
