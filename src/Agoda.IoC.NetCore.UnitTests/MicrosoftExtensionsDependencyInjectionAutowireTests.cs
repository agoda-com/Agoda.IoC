﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Agoda.IoC.ProjectUnderTest.Valid;
using Agoda.IoC.ProjectUnderTest.Valid2;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace Agoda.IoC.NetCore.UnitTests
{
    [TestFixture]
    public class MicrosoftExtensionsDependencyInjectionAutowireTests
    {
        private IServiceCollection _container;
        private IServiceCollection _notReplaceContainer;

        private IServiceCollection _containerMocked;
        [SetUp]
        public void SetUp()
        {
            _container = new ServiceCollection();
            _notReplaceContainer = new ServiceCollection();
            _container.AutoWireAssembly(new[]
            {
                typeof(NoAttribute).Assembly,
                typeof(ReplaceServiceTwoWork).Assembly,
            }, false);
            _notReplaceContainer.AutoWireAssembly(new[]
           {
                typeof(NoAttribute).Assembly
            }, false);

            _containerMocked = new ServiceCollection();
            _containerMocked.AutoWireAssembly(new[]
            {
                typeof(NoAttribute).Assembly
            }, true);
        }

        [Test]
        public void LookforAutowire_ConcreteImplementation()
        {
            _container
                .Any(x =>
                    x.ServiceType == typeof(ConcreteImplementation)
                    && x.Lifetime == ServiceLifetime.Scoped)
                .ShouldBeTrue();
        }

        [Test]
        public void LookforAutowire_IService()
        {
            _container
                .Any(x =>
                    x.ServiceType == typeof(IService)
                    && x.ImplementationType == typeof(Service)
                    && x.Lifetime == ServiceLifetime.Scoped)
                .ShouldBeTrue();
        }

        [Test]
        public void LookforAutowire_MockServiceOriginal()
        {
            _container
                .Any(x =>
                    x.ServiceType == typeof(IServiceWithMock)
                    && x.ImplementationType == typeof(ServiceWithMock)
                    && x.Lifetime == ServiceLifetime.Transient)
                .ShouldBeTrue();
        }

        [Test]
        public void LookforAutowire_MockServiceMock()
        {
            _containerMocked
                .Any(x =>
                    x.ServiceType == typeof(IServiceWithMock)
                    && x.ImplementationType == typeof(MockService)
                    && x.Lifetime == ServiceLifetime.Transient)
                .ShouldBeTrue();
        }

        [Test]
        public void LookforAutowire_IInterfaceThatShouldNotGetRegistered()
        {
            _container
                .Any(x =>
                    x.ServiceType == typeof(IInterfaceThatShouldNotGetRegistered))
                .ShouldBeFalse();
        }

        [Test]
        public void LookforAutowire_IExplicitlyRegisteredInterface()
        {
            _container
                .Any(x =>
                    x.ServiceType == typeof(IExplicitlyRegisteredInterface)
                    && x.ImplementationType == typeof(ServiceWithExplicitInterfaceRegistration)
                    && x.Lifetime == ServiceLifetime.Transient)
                .ShouldBeTrue();
        }

        [Test]
        public void LookforAutowire_IDisposable()
        {
            _container
                .Any(x =>
                    x.ServiceType == typeof(IDisposable))
                .ShouldBeFalse();
        }

        [Test]
        public void LookforAutowire_IServiceThatImplementsInterfaceFromMscorlib()
        {
            _container
                .Any(x =>
                    x.ServiceType == typeof(IServiceThatImplementsInterfaceFromMscorlib)
                    && x.ImplementationType == typeof(ServiceThatImplementsInterfaceFromMscorlib)
                    && x.Lifetime == ServiceLifetime.Transient)
                .ShouldBeTrue();
        }

        [Test]
        public void LookforAutowire_MyInheritedClass()
        {
            _container
                .Any(x =>
                    x.ServiceType == typeof(MyBaseClass)
                    && x.ImplementationType == typeof(MyInheritedClass)
                    && x.Lifetime == ServiceLifetime.Singleton)
                .ShouldBeTrue();
        }

        [Test]
        public void LookforAutowire_Concrete()
        {
            _container
                .Any(x =>
                    x.ServiceType == typeof(Concrete)
                    && x.ImplementationType == typeof(Concrete)
                    && x.Lifetime == ServiceLifetime.Singleton)
                .ShouldBeTrue();
        }

        [Test]
        public void LookforAutowire_ITransientFromFactory()
        {
            _container
                .Any(x =>
                    x.ServiceType == typeof(ITransientFromFactory)
                    && x.ImplementationFactory != null
                    && x.Lifetime == ServiceLifetime.Transient)
                .ShouldBeTrue();
        }

        [Test]
        public void LookforAutowire_IFactoryAndMock()
        {
            _container
                .Any(x =>
                    x.ServiceType == typeof(IFactoryAndMock)
                    && x.ImplementationFactory != null // TODO need to improve this and add check in mock
                    && x.Lifetime == ServiceLifetime.Transient)
                .ShouldBeTrue();
        }

        [Test]
        public void LookforAutowire_IMultipleAttributes2()
        {
            _container
                .Any(x =>
                    x.ServiceType == typeof(IMultipleAttributes2)
                    && x.ImplementationType == typeof(MultipleAttributes)
                    && x.Lifetime == ServiceLifetime.Singleton)
                .ShouldBeTrue();
        }

        [Test]
        public void LookforAutowire_IMultipleAttributes1()
        {
            _container
                .Any(x =>
                    x.ServiceType == typeof(IMultipleAttributes1)
                    && x.ImplementationType == typeof(MultipleAttributes)
                    && x.Lifetime == ServiceLifetime.Singleton)
                .ShouldBeTrue();
        }

        [Test]
        public void LookforAutowire_IOpenGenericService()
        {
            _container
                .Any(x =>
                    x.ServiceType == typeof(IOpenGenericService<>)
                    && x.ImplementationType == typeof(OpenGenericService<>)
                    && x.Lifetime == ServiceLifetime.Singleton)
                .ShouldBeTrue();
        }

        [Test]
        public void LookforAutowire_IClosedGenericService()
        {
            _container
                .Any(x =>
                    x.ServiceType == typeof(IClosedGenericService<List<int>>)
                    && x.ImplementationType == typeof(ClosedGenericService<List<int>>)
                    && x.Lifetime == ServiceLifetime.Singleton)
                .ShouldBeTrue();
        }

        [Test]
        public void LookforAutowire_GenericWithFactory()
        {
            _container
                .Any(x =>
                    x.ServiceType == typeof(IGenericWithFactory<ArrayList>)
                    && x.ImplementationFactory != null
                    && x.Lifetime == ServiceLifetime.Transient)
                .ShouldBeTrue();
            _container
                .Any(x =>
                    x.ServiceType == typeof(IGenericWithFactory<Hashtable>)
                    && x.ImplementationFactory != null
                    && x.Lifetime == ServiceLifetime.Transient)
                .ShouldBeTrue();
            _container
                .Any(x =>
                    x.ServiceType == typeof(IGenericWithFactory<Stack>)
                    && x.ImplementationFactory != null
                    && x.Lifetime == ServiceLifetime.Transient)
                .ShouldBeTrue();
        }

        [Test]
        public void LookforAutowire_KeyedRegistrationFactoryChecks()
        {
            _container
                .Any(x =>
                    x.ServiceType == typeof(KeyedFactoryService1)
                    && x.Lifetime == ServiceLifetime.Singleton)
                .ShouldBeTrue();
            _container
                .Any(x =>
                    x.ServiceType == typeof(KeyedFactoryService2)
                    && x.Lifetime == ServiceLifetime.Scoped)
                .ShouldBeTrue();
        }

        [Test]
        public void LookforAutowire_ReplaceServiceChecks()
        {
            _container
                .Any(x =>
                    x.ServiceType == typeof(IReplaceService)
                    && x.ImplementationType == typeof(ReplaceServiceTwoWork)
                    && x.Lifetime == ServiceLifetime.Transient)
                .ShouldBeTrue();

            _notReplaceContainer
                .Any(x =>
                    x.ServiceType == typeof(IReplaceService)
                    && x.ImplementationType == typeof(ReplaceServiceOneWork)
                    && x.Lifetime == ServiceLifetime.Transient)
                .ShouldBeTrue();


            var svr = _container.BuildServiceProvider().GetRequiredService<IReplaceService>();
            svr.DoWork.ShouldBe(nameof(ReplaceServiceTwoWork));

            svr = _notReplaceContainer.BuildServiceProvider().GetRequiredService<IReplaceService>();
            svr.DoWork.ShouldBe(nameof(ReplaceServiceOneWork));
        }
    }
}
