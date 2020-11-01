using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Agoda.IoC.Core;
using Agoda.IoC.ProjectUnderTest;
using Agoda.IoC.ProjectUnderTest.Invalid1;
using Agoda.IoC.ProjectUnderTest.Invalid10;
using Agoda.IoC.ProjectUnderTest.Invalid11;
using Agoda.IoC.ProjectUnderTest.Invalid12;
using Agoda.IoC.ProjectUnderTest.Invalid13;
using Agoda.IoC.ProjectUnderTest.Invalid14;
using Agoda.IoC.ProjectUnderTest.Invalid15;
using Agoda.IoC.ProjectUnderTest.Invalid16;
using Agoda.IoC.ProjectUnderTest.Invalid17;
using Agoda.IoC.ProjectUnderTest.Invalid18;
using Agoda.IoC.ProjectUnderTest.Invalid2;
using Agoda.IoC.ProjectUnderTest.Invalid3;
using Agoda.IoC.ProjectUnderTest.Invalid4;
using Agoda.IoC.ProjectUnderTest.Invalid5;
using Agoda.IoC.ProjectUnderTest.Invalid6;
using Agoda.IoC.ProjectUnderTest.Invalid7;
using Agoda.IoC.ProjectUnderTest.Invalid8;
using Agoda.IoC.ProjectUnderTest.Invalid9;
using Agoda.IoC.ProjectUnderTest.Valid;
using NUnit.Framework;
using Microsoft.Practices.Unity;
using MockService = Agoda.IoC.ProjectUnderTest.Valid.MockService;

namespace Agoda.IoC.Unity.UnitTests
{

    [TestFixture]
    public class UnityContainerAttributeExtensionsTest
    {
        private UnityContainer _container;

        [SetUp]
        public void SetUp()
        {
            _container = new UnityContainer();
        }

        [TearDown]
        public void TearDown()
        {
            _container.Dispose();
        }

        [Test]
        public void RegisterByAttribute_WhenNoAttribute_DoesNotRegister()
        {
            _container.RegisterByAttribute(false, typeof(INoAttribute).Assembly);

            Assert.IsFalse(_container.IsRegistered<INoAttribute>());
        }

        [Test]
        public void RegisterByAttribute_ForServiceWithNoMockImplementation_AlwaysRegistersDefaultImplementation([Values] bool mockMode)
        {
            _container.RegisterByAttribute(mockMode, typeof(IService).Assembly);

            var result = _container.Resolve<IService>();

            Assert.AreEqual(typeof(Service), result.GetType());
        }

        [Test]
        [TestCase(false, typeof(ServiceWithMock))]
        [TestCase(true, typeof(MockService))]
        public void RegisterByAttribute_ForServiceWithMockImplementation_RegistersAppropriateImplementation(bool mockMode, Type expectedType)
        {
            _container.RegisterByAttribute(mockMode, typeof(IService).Assembly);

            var result = _container.Resolve<IServiceWithMock>();

            Assert.AreEqual(expectedType, result.GetType());
        }

        [Test]
        public void RegisterByAttribute_ForConcreteImplementation_RegistersImplementation()
        {
            _container.RegisterByAttribute(false, typeof(ConcreteImplementation).Assembly);

            var result = _container.Resolve<ConcreteImplementation>();

            Assert.AreEqual(typeof(ConcreteImplementation), result.GetType());
        }

        /// It doesn't make sense to specify a mock for concrete implementation.
        [Test]
        public void RegisterByAttribute_ForConcreteImplementationWithMock_Throws()
        {
            Assert.Throws<RegistrationFailedException>(
                () => _container.RegisterByAttribute(false, typeof(ConcreteImplementationWithMock).Assembly));
        }

        [Test]
        public void RegisterByAttribute_ForServiceWithExplicitInterfacesDefined_RegistersOnlyThose()
        {
            _container.RegisterByAttribute(false, typeof(IExplicitlyRegisteredInterface).Assembly);

            var result = _container.Resolve<IExplicitlyRegisteredInterface>();

            Assert.AreEqual(typeof(ServiceWithExplicitInterfaceRegistration), result.GetType());
            Assert.Throws<ResolutionFailedException>(() => _container.Resolve<IInterfaceThatShouldNotGetRegistered>());
        }

        [Test]
        public void RegisterByAttribute_ForInterfaceDefinedInMscorlib_DoesntRegister()
        {
            _container.RegisterByAttribute(false, typeof(IServiceThatImplementsInterfaceFromMscorlib).Assembly);

            var result = _container.Resolve<IServiceThatImplementsInterfaceFromMscorlib>();

            Assert.AreEqual(typeof(ServiceThatImplementsInterfaceFromMscorlib), result.GetType());
            Assert.Throws<ResolutionFailedException>(() => _container.Resolve<IDisposable>());
        }

        [Test]
        public void RegisterByAttribute_WhenAbstractTypeIsReRegistered_Throws()
        {
            Assert.Throws<RegistrationFailedException>(
                () => _container.RegisterByAttribute(false, typeof(IAbstractReregistered).Assembly)
            );
        }

        [Test]
        public void RegisterByAttribute_WhenConcreteTypeIsReRegistered_Throws()
        {
            Assert.Throws<RegistrationFailedException>(
                () => _container.RegisterByAttribute(false, typeof(ConcreteReregistered).Assembly)
            );
        }

        [Test]
        public void RegisterByAttribute_ForTypeDerivedFromBaseClass_Registers()
        {
            _container.RegisterByAttribute(false, typeof(MyBaseClass).Assembly);

            var result = _container.Resolve<MyBaseClass>();

            Assert.AreEqual(typeof(MyInheritedClass), result.GetType());
        }

        [Test]
        public void RegisterByAttribute_WithAmbiguousRegistration_Throws()
        {
            Assert.Throws<RegistrationFailedException>(() =>
                _container.RegisterByAttribute(false, typeof(AmbiguousRegistration).Assembly)
            );
        }

        [Test]
        public void RegisterByAttribute_ForCollection_RegistersInOrder()
        {
            _container.RegisterByAttribute(false, typeof(ISingletonCollectionItem).Assembly);

            var result = _container.Resolve<IEnumerable<ISingletonCollectionItem>>().ToArray();

            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(typeof(SingletonCollectionClass2), result[0].GetType());
            Assert.AreEqual(typeof(SingletonCollectionClass1), result[1].GetType());
        }

        [Test]
        public void RegisterByAttribute_ForTransientCollection_ResolvesDifferentInstance()
        {
            _container.RegisterByAttribute(false, typeof(ITransientCollectionItem).Assembly);

            var result1 = _container.Resolve<IEnumerable<ITransientCollectionItem>>();
            var result2 = _container.Resolve<IEnumerable<ITransientCollectionItem>>();

            Assert.AreNotSame(result1, result2);
        }

        [Test]
        public void RegisterByAttribute_ForSingletonCollection_ResolvesSameInstance()
        {
            _container.RegisterByAttribute(false, typeof(ISingletonCollectionItem).Assembly);

            var result1 = _container.Resolve<IEnumerable<ISingletonCollectionItem>>();
            var result2 = _container.Resolve<IEnumerable<ISingletonCollectionItem>>();

            Assert.AreSame(result1, result2);
        }

        [Test]
        public void RegisterByAttribute_ForOrderWithoutCollection_Throws()
        {
            Assert.Throws<RegistrationFailedException>(() =>
                _container.RegisterByAttribute(false, typeof(IOrderWithoutCollection).Assembly)
            );
        }

        [Test]
        public void RegisterByAttribute_ForCollectionWithMismatchedLifetimes_Throws()
        {
            Assert.Throws<RegistrationFailedException>(() =>
                _container.RegisterByAttribute(false, typeof(IMismatchedCollectionLifetime).Assembly)
            );
        }

        [Test]
        public void RegisterByAttribute_ForNonUniquelyOrderedCollection_Throws()
        {
            Assert.Throws<RegistrationFailedException>(() =>
                _container.RegisterByAttribute(false, typeof(ICollectionWithNonUniqueOrder).Assembly)
            );
        }

        [Test]
        public void RegisterByAttribute_Concrete_RegistersConcrete()
        {
            _container.RegisterByAttribute(false, typeof(IConcrete).Assembly);

            var result = _container.Resolve<Concrete>();

            Assert.AreEqual(typeof(Concrete), result.GetType());
            Assert.Throws<ResolutionFailedException>(() => _container.Resolve<IConcrete>());
        }

        [Test]
        public void RegisterByAttribute_WithKeyedFactoryAndValidKey_Resolves()
        {
            _container.RegisterByAttribute(false, typeof(IKeyedFactoryService).Assembly);

            var factory = _container.Resolve<IKeyedComponentFactory<IKeyedFactoryService>>();

            var service1 = factory.GetByKey("Service_1");
            var service2 = factory.GetByKey("Service_2");

            Assert.AreEqual(typeof(KeyedFactoryService1), service1.GetType());
            Assert.AreEqual(typeof(KeyedFactoryService2), service2.GetType());
        }

        [Test]
        public void RegisterByAttribute_WithKeyedFactoryAndInvalidKey_Throws()
        {
            _container.RegisterByAttribute(false, typeof(IKeyedFactoryService).Assembly);

            var factory = _container.Resolve<IKeyedComponentFactory<IKeyedFactoryService>>();

            Assert.That(() => factory.GetByKey("Service_3"), Throws.Exception);
        }

        [Test]
        public void RegisterByAttribute_WithKeyedFactoryAndOfCollection_Throws()
        {
            Assert.Throws<RegistrationFailedException>(
                () => _container.RegisterByAttribute(false, typeof(IHybridOfCollectionFactoryService).Assembly));
        }

        [Test]
        public void RegisterByAttribute_WithFactory_ConstructsByFactory()
        {
            _container.RegisterByAttribute(false, typeof(ITransientFromFactory).Assembly);

            var result1 = _container.Resolve<ITransientFromFactory>();

            Assert.AreEqual(typeof(TransientFromFactory), result1.GetType());
            Assert.AreEqual("test", result1.Test);
        }

        [Test]
        public void RegisterByAttribute_WithTransientAndFactory_ConstructsNewInstances()
        {
            _container.RegisterByAttribute(false, typeof(ITransientFromFactory).Assembly);

            var result1 = _container.Resolve<ITransientFromFactory>();
            var result2 = _container.Resolve<ITransientFromFactory>();

            Assert.AreNotSame(result1, result2);
        }

        [Test]
        public void RegisterByAttribute_WithSingletonAndFactory_ResolvesSameInstance()
        {
            _container.RegisterByAttribute(false, typeof(ISingletonFromFactory).Assembly);

            var result1 = _container.Resolve<ISingletonFromFactory>();
            var result2 = _container.Resolve<ISingletonFromFactory>();

            Assert.AreSame(result1, result2);
        }

        [TestCase(false, typeof(FactoryAndMock))]
        [TestCase(true, typeof(MockFactoryAndMock))]
        public void RegisterByAttribute_WithFactoryAndMock_ResolvesCorrectType(bool mockMode, Type expectedType)
        {
            _container.RegisterByAttribute(mockMode, typeof(IFactoryAndMock).Assembly);

            var result1 = _container.Resolve<IFactoryAndMock>();

            Assert.AreEqual(expectedType, result1.GetType());
        }

        [Test]
        public void RegisterByAttribute_WithConcreteFactory_ResolvesConcreteInstances()
        {
            _container.RegisterByAttribute(false, typeof(ConcreteFromFactory).Assembly);

            var result1 = _container.Resolve<ConcreteFromFactory>();
            var result2 = _container.Resolve<ConcreteFromFactory>();

            Assert.AreEqual("concrete", result1.Test);
            Assert.AreEqual("concrete", result2.Test);
            Assert.AreNotSame(result1, result2);
            Assert.AreEqual(result1.GetType(), typeof(ConcreteFromFactory));
        }

        [Test]
        public void RegisterByAttribute_WithIncompatibleMockType_Throws()
        {
            Assert.Throws<RegistrationFailedException>(() =>
                _container.RegisterByAttribute(true, typeof(IIncompatibleMock).Assembly));
        }

        [Test]
        public void RegisterByAttribute_WithMockThatDerivesFromLive_Throws()
        {
            Assert.Throws<RegistrationFailedException>(() =>
                _container.RegisterByAttribute(true, typeof(IMockThatDerivesFromLive).Assembly)
            );
        }

        [Test]
        public void RegisterByAttribute_WithInterfaceForMock_Throws()
        {
            Assert.Throws<RegistrationFailedException>(() =>
                _container.RegisterByAttribute(true, typeof(IServiceWithInterfaceForMock).Assembly)
            );
        }

        [Test]
        public void RegisterByAttribute_WithRecursiveDefinition_Throws()
        {
            Assert.Throws<RegistrationFailedException>(() =>
                _container.RegisterByAttribute(true, typeof(IRecursive).Assembly)
            );
        }

        [Test]
        public void RegisterByAttribute_WithMultipleAttributes_CanResolveAll()
        {
            _container.RegisterByAttribute(false, typeof(MultipleAttributes).Assembly);

            var result1 = _container.Resolve<IMultipleAttributes1>();
            var result2 = _container.Resolve<IMultipleAttributes2>();

            Assert.AreEqual(typeof(MultipleAttributes), result1.GetType());
            Assert.AreSame(result1, result2);
        }

        [Test]
        public void RegisterByAttribute_ForInterceptedWithAmbiguousBaseType_Throws()
        {
            Assert.Throws<RegistrationFailedException>(() =>
                _container.RegisterByAttribute(false, typeof(IAmbiguousIntercepted).Assembly)
            );
        }

        [Test]
        public void RegisterByAttribute_WithOpenGeneric_Resolves()
        {
            _container.RegisterByAttribute(false, typeof(IOpenGenericService<>).Assembly);

            var result1 = _container.Resolve<IOpenGenericService<Dictionary<int, int>>>().GetValue();
            var result2 = _container.Resolve<IOpenGenericService<List<int>>>().GetValue();

            Assert.AreEqual(typeof(Dictionary<int, int>), result1.GetType());
            Assert.AreEqual(typeof(List<int>), result2.GetType());
        }

        [Test]
        public void RegisterByAttribute_WithClosedGeneric_Resolves()
        {
            _container.RegisterByAttribute(false, typeof(IClosedGenericService<>).Assembly);

            var result = _container.Resolve<IClosedGenericService<List<int>>>().GetValue();

            Assert.AreEqual(typeof(List<int>), result.GetType());
        }

        [Test]
        public void RegisterByAttribute_ForGenericWithFactory_Resolves()
        {
            _container.RegisterByAttribute(false, typeof(IGenericWithFactory<>).Assembly);

            var result1 = _container.Resolve<IGenericWithFactory<ArrayList>>();
            var result2 = _container.Resolve<IGenericWithFactory<Hashtable>>();
            var result3 = _container.Resolve<IGenericWithFactory<Stack>>();

            Assert.AreEqual(typeof(GenericWithFactory<ArrayList>), result1.GetType());
            Assert.AreEqual(typeof(GenericWithFactory<Hashtable>), result2.GetType());
            Assert.AreEqual(typeof(GenericWithFactory<Stack>), result3.GetType());
            Assert.AreEqual("Factory1", result1.BuiltBy);
            Assert.AreEqual("Factory1", result2.BuiltBy);
            Assert.AreEqual("Factory2", result3.BuiltBy);
        }

        [Test]
        public void RegisterByAttribute_ForGenericWithFactoryWithMultipleGenericParams_Throws()
        {
            Assert.Throws<RegistrationFailedException>(() =>
                _container.RegisterByAttribute(false, typeof(IGenericWithFactoryAndMultipleGenericParams<,>).Assembly)
            );
        }

        [Test]
        public void RegisterByAttribute_ForGenericWithFactoryAndMissingGenericArgument_Throws()
        {
            Assert.Throws<RegistrationFailedException>(() =>
                _container.RegisterByAttribute(false, typeof(IGenericWithFactoryAndMissingGenericArgument<>).Assembly)
            );
        }

        [Test]
        public void RegisterByAttribute_ForGenericWithFactoryAndGenericArgument_Throws()
        {
            Assert.Throws<RegistrationFailedException>(() =>
                _container.RegisterByAttribute(false, typeof(IGenericWithFactoryAndGenericArgumentThatIsItselfGeneric<>).Assembly)
            );
        }

    }
}
