using System;
using System.Collections.Generic;
using Agoda.IoC.Core;

namespace Agoda.IoC.ProjectUnderTest.Invalid18
{
    public class GenericWithFactoryAndGenericArgumentThatIsItselfGenericFactory<T1> : IComponentFactory<IGenericWithFactoryAndGenericArgumentThatIsItselfGeneric<T1>>
    {
        public IGenericWithFactoryAndGenericArgumentThatIsItselfGeneric<T1> Build(IComponentResolver c)
        {
            throw new NotImplementedException();
        }
    }
    
    public interface IGenericWithFactoryAndGenericArgumentThatIsItselfGeneric<in T> {}

    [RegisterTransient(Factory = typeof(GenericWithFactoryAndGenericArgumentThatIsItselfGenericFactory<>), GenericArgument = typeof(Stack<>))]
    public class GenericWithFactoryAndGenericArgumentThatIsItselfGeneric<T1> : IGenericWithFactoryAndGenericArgumentThatIsItselfGeneric<T1> {}
}
