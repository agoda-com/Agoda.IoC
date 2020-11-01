using Agoda.IoC.Core;
using System;
using System.Collections;
using System.Collections.Generic;


// These services are used to test registering by attribute.
namespace Agoda.IoC.ProjectUnderTest.Valid
{
    // no attribute
    public interface INoAttribute{}
    public class NoAttribute: INoAttribute {}

    // concrete implementation
    [RegisterPerRequest]
    public class ConcreteImplementation
    {
    }
    
    // Simple registration
    public interface IService {}
    [RegisterPerRequest]
    public class Service : IService {}
    
    // Mocked registration
    public class MockService : IServiceWithMock {}
    public interface IServiceWithMock{}
    [RegisterTransient(Mock = typeof(MockService))]
    public class ServiceWithMock : IServiceWithMock {}

    // Explicit registration
    public interface IExplicitlyRegisteredInterface {}
    public interface IInterfaceThatShouldNotGetRegistered {}
    // This class implements 2 interfaces, but we explicitly tell it to register only 1.
    [RegisterTransient(For = typeof(IExplicitlyRegisteredInterface))]
    public class ServiceWithExplicitInterfaceRegistration : IExplicitlyRegisteredInterface, IInterfaceThatShouldNotGetRegistered {}

    // This class implements an interface from mscorlib, which we shouldn't register.
    public interface IServiceThatImplementsInterfaceFromMscorlib {}
    [RegisterTransient]
    public class ServiceThatImplementsInterfaceFromMscorlib : IServiceThatImplementsInterfaceFromMscorlib, IDisposable
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
    
    // Base classes should register against derived classes
    public class MyBaseClass{}
    [RegisterSingleton]
    public class MyInheritedClass : MyBaseClass {}
    
    // Checking collections work
    public interface ISingletonCollectionItem {}
    [RegisterSingleton(OfCollection = true, Order = 1)]
    public class SingletonCollectionClass1 : ISingletonCollectionItem {}
    [RegisterSingleton(OfCollection = true, Order = 0)]
    public class SingletonCollectionClass2 : ISingletonCollectionItem {}
    
    // Checking collections work
    public interface ITransientCollectionItem {}
    [RegisterTransient(OfCollection = true)]
    public class TransientCollectionClass1 : ITransientCollectionItem {}
    [RegisterTransient(OfCollection = true)]
    public class TransientCollectionClass2 : ITransientCollectionItem {}
    
    // Checking Concrete
    public interface IConcrete {}
    [RegisterSingleton(Concrete = true)]
    public class Concrete : IConcrete {}
    
    // Checking keyed factory
    public interface IKeyedFactoryService {}
    [RegisterSingleton(Key = "Service_1")]
    public class KeyedFactoryService1 : IKeyedFactoryService {} 
    [RegisterPerRequest(Key = "Service_2")]
    public class KeyedFactoryService2 : IKeyedFactoryService {}

    // Checking component factory
    
    public class MyTransientFactory : IComponentFactory<ITransientFromFactory>
    {
        public ITransientFromFactory Build(IComponentResolver c)
        {
            return new TransientFromFactory("test");
        }
    }

    public interface ITransientFromFactory { string Test { get; } }
    [RegisterTransient(Factory = typeof(MyTransientFactory))]
    public class TransientFromFactory : ITransientFromFactory
    {
        public string Test { get; }

        public TransientFromFactory(string test)
        {
            Test = test;
        }
    }
    
    public class MySingletonFactory : IComponentFactory<ISingletonFromFactory>
    {
        public ISingletonFromFactory Build(IComponentResolver c)
        {
            return new SingletonFromFactory("test");
        }
    }
    
    public interface ISingletonFromFactory { string Test { get; } }
    [RegisterSingleton(Factory = typeof(MySingletonFactory))]
    public class SingletonFromFactory : ISingletonFromFactory
    {
        public string Test { get; }

        public SingletonFromFactory(string test)
        {
            Test = test;
        }
    }
    
    // checking component factory with mock
    public interface IConstructedByFactoryWithMock { string Test { get; } }
    public class FactoryForComponentWithMock : IComponentFactory<IConstructedByFactoryWithMock>
    {
        public IConstructedByFactoryWithMock Build(IComponentResolver c)
        {
            // should not be called in mock mode
            throw new NotImplementedException();
        }
    }
   
    
    public class FactoryAndMockFactory : IComponentFactory<IFactoryAndMock> 
    {
        public IFactoryAndMock Build(IComponentResolver c)
        {
            return new FactoryAndMock();
        }
    }
    
    public interface IFactoryAndMock { }
    public class MockFactoryAndMock : IFactoryAndMock {}
    [RegisterTransient(Factory = typeof(FactoryAndMockFactory), Mock = typeof(MockFactoryAndMock))]
    public class FactoryAndMock : IFactoryAndMock { }
    
    
    public class MyConcreteFactory : IComponentFactory<ConcreteFromFactory>
    {
        public ConcreteFromFactory Build(IComponentResolver c)
        {
            return new ConcreteFromFactory("concrete"); 
        }
    }

    [RegisterTransient(Factory = typeof(MyConcreteFactory), Concrete = true)]
    public class ConcreteFromFactory : ITransientFromFactory
    {
        public string Test { get; }
        
        public ConcreteFromFactory(string test)
        {
            Test = test;
        }
    }
    
    
    // testing multiple attributes
    
    public interface IMultipleAttributes1 {}
    public interface IMultipleAttributes2 {}
    [RegisterSingleton(For = typeof(IMultipleAttributes1))]
    [RegisterSingleton(For = typeof(IMultipleAttributes2))]
    public class MultipleAttributes : IMultipleAttributes1, IMultipleAttributes2 {}
    
    
    // testing open generics

    public interface IOpenGenericService<T> where T : new()
    {
        T GetValue();
    }
    
    [RegisterSingleton(For = typeof(IOpenGenericService<>))]
    public class OpenGenericService<T> : IOpenGenericService<T> 
        where T : new()
    {
        public T GetValue()
        {
            return new T();
        }
    }
    
    
        
    // testing closed generics

    public interface IClosedGenericService<T> where T : new()
    {
        T GetValue();
    }
    
    // TODO: this syntax is redundant but required right now to make this work. It's not a major use-case, but it 
    // would be nice to eliminate the GenericArgument parameter that is currently required. I'm sure it's possible.
    [RegisterSingleton(For = typeof(IClosedGenericService<List<int>>), GenericArgument = typeof(List<int>))]
    public class ClosedGenericService<T> : IClosedGenericService<T> 
        where T : new()
    {
        public T GetValue()
        {
            return new T();
        }
    }

   
    // testing generics with factory
    
    public class GenericWithFactoryFactory1<T> : IComponentFactory<IGenericWithFactory<T>>
    {
        public IGenericWithFactory<T> Build(IComponentResolver c)
        {
            var result = new GenericWithFactory<T> { BuiltBy = "Factory1" };
            return result;
        }
    }

    public class GenericWithFactoryFactory2<T> : IComponentFactory<IGenericWithFactory<T>>
    {
        public IGenericWithFactory<T> Build(IComponentResolver c)
        {
            var result = new GenericWithFactory<T> { BuiltBy = "Factory2" };
            return result;
        }
    }
    
    public interface IGenericWithFactory<in T>
    {
        string BuiltBy { get; set; }
    }

    [RegisterTransient(Factory = typeof(GenericWithFactoryFactory1<>), GenericArgument = typeof(ArrayList))]
    [RegisterTransient(Factory = typeof(GenericWithFactoryFactory1<>), GenericArgument = typeof(Hashtable))]
    [RegisterTransient(Factory = typeof(GenericWithFactoryFactory2<>), GenericArgument = typeof(Stack))]
    public class GenericWithFactory<T> : IGenericWithFactory<T>
    {
        public string BuiltBy { get; set; }
    }
    
}
