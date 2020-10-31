using System;
using System.Collections;
using System.Collections.Generic;
using Agoda.IoC.Core;

namespace Agoda.IoC.ProjectUnderTest.Invalid17
{
    public class GenericWithFactoryFactory<T1, T2> : IComponentFactory<IGenericWithFactoryAndMultipleGenericParams<T1, T2>>
    {
        public IGenericWithFactoryAndMultipleGenericParams<T1, T2> Build(IComponentResolver c)
        {
            throw new NotImplementedException();
        }
    }
    
    public interface IGenericWithFactoryAndMultipleGenericParams<in T, in T2> {}

    [RegisterTransient(Factory = typeof(GenericWithFactoryFactory<,>), GenericArgument = typeof(Stack))]
    public class GenericWithFactoryAndMultipleGenericParams<T1, T2> : IGenericWithFactoryAndMultipleGenericParams<T1, T2> {}
}
