using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Agoda.IoC.ProjectUnderTest.Valid;
using Autofac;
using Autofac.Core;
using Autofac.Core.Lifetime;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;
using Service = Agoda.IoC.ProjectUnderTest.Valid.Service;

namespace Agoda.IoC.AutofacExt.UnitTests
{
    public class AutoWireAssemblyExtTests
    {
        private IContainer _container;
        private IContainer _containerMocked;

        private ContainerBuilder _containerBuilder;
        private ContainerBuilder _containerBuilderMocked;

        [SetUp]
        public void SetUp()
        {
            _containerBuilder = new ContainerBuilder();
            _container = _containerBuilder.AutoWireAssembly(new[]
            {
                typeof(NoAttribute).Assembly
            }, false).Build();
            _containerBuilderMocked = new ContainerBuilder();
            _containerMocked = _containerBuilderMocked.AutoWireAssembly(new[]
            {
                typeof(NoAttribute).Assembly
            }, true).Build();
        }

        [Test]
        public void LookforAutowire_ConcreteImplementation()
        {
            _container.ComponentRegistry.Registrations
                .Any(x => x.Activator.LimitType == typeof(ConcreteImplementation)
                && x.Lifetime is CurrentScopeLifetime)
                .ShouldBeTrue();
        }

        [Test]
        public void LookforAutowire_IService()
        {
            _container.ComponentRegistry.Registrations
                .Any(x =>
                    ContainsType(x.Services,typeof(IService))
                    && x.Activator.LimitType == typeof(Service)
                    && x.Lifetime is CurrentScopeLifetime)
                .ShouldBeTrue();
        }

        private bool ContainsType(IEnumerable<Autofac.Core.Service> services, Type t)
        {
            return services.Where(y => y is TypedService)
                .Any(z => ((TypedService) z).ServiceType == t);
        }

        [Test]
        public void LookforAutowire_MockServiceOriginal()
        {
            _container.ComponentRegistry.Registrations
                .Any(x =>
                    ContainsType(x.Services, typeof(IServiceWithMock))
                    && x.Activator.LimitType == typeof(ServiceWithMock)
                    && x.Lifetime is CurrentScopeLifetime)
                .ShouldBeTrue();
        }

        [Test]
        public void LookforAutowire_MockServiceMock()
        {
            _containerMocked.ComponentRegistry.Registrations
                .Any(x =>
                    ContainsType(x.Services, typeof(IServiceWithMock))
                    && x.Activator.LimitType == typeof(MockService)
                    && x.Lifetime is CurrentScopeLifetime)
                .ShouldBeTrue();
        }

        [Test]
        public void LookforAutowire_IInterfaceThatShouldNotGetRegistered()
        {
            _container.ComponentRegistry.Registrations
                .Any(x =>
                    ContainsType(x.Services, typeof(IInterfaceThatShouldNotGetRegistered)))
                .ShouldBeFalse();
        }

        [Test]
        public void LookforAutowire_IExplicitlyRegisteredInterface()
        {
            _container.ComponentRegistry.Registrations
                .Any(x =>
                    ContainsType(x.Services, typeof(IExplicitlyRegisteredInterface))
                    && x.Activator.LimitType == typeof(ServiceWithExplicitInterfaceRegistration)
                    && x.Lifetime is CurrentScopeLifetime)
                .ShouldBeTrue();
        }

        [Test]
        public void LookforAutowire_IDisposable()
        {
            _container.ComponentRegistry.Registrations
                .Any(x =>
                    ContainsType(x.Services, typeof(IDisposable)))
                .ShouldBeFalse();
        }

        [Test]
        public void LookforAutowire_IServiceThatImplementsInterfaceFromMscorlib()
        {
            _container.ComponentRegistry.Registrations
                .Any(x =>
                    ContainsType(x.Services, typeof(IServiceThatImplementsInterfaceFromMscorlib))
                    && x.Activator.LimitType == typeof(ServiceThatImplementsInterfaceFromMscorlib)
                    && x.Lifetime is CurrentScopeLifetime)
                .ShouldBeTrue();
        }

        [Test]
        public void LookforAutowire_MyInheritedClass()
        {
            _container.ComponentRegistry.Registrations
                .Any(x =>
                    ContainsType(x.Services, typeof(MyBaseClass))
                    && x.Activator.LimitType == typeof(MyInheritedClass)
                    && x.Lifetime is RootScopeLifetime)
                .ShouldBeTrue();
        }

        [Test]
        public void LookforAutowire_Concrete()
        {
            _container.ComponentRegistry.Registrations
                .Any(x =>
                    ContainsType(x.Services, typeof(Concrete))
                    && x.Activator.LimitType == typeof(Concrete)
                    && x.Lifetime is RootScopeLifetime)
                .ShouldBeTrue();
        }

        [Test]
        public void LookforAutowire_ITransientFromFactory()
        {
            _container.ComponentRegistry.Registrations
                .Any(x =>
                    ContainsType(x.Services, typeof(ITransientFromFactory))
                    && x.Activator.LimitType != null
                    && x.Lifetime is CurrentScopeLifetime)
                .ShouldBeTrue();
        }

        [Test]
        public void LookforAutowire_IFactoryAndMock()
        {
            _container.ComponentRegistry.Registrations
                .Any(x =>
                    ContainsType(x.Services, typeof(IFactoryAndMock))
                    && x.Activator.LimitType != null // TODO need to improve this and add check in mock
                    && x.Lifetime is CurrentScopeLifetime)
                .ShouldBeTrue();
        }

        [Test]
        public void LookforAutowire_IMultipleAttributes2()
        {
            _container.ComponentRegistry.Registrations
                .Any(x =>
                    ContainsType(x.Services, typeof(IMultipleAttributes2))
                    && x.Activator.LimitType == typeof(MultipleAttributes)
                    && x.Lifetime is RootScopeLifetime)
                .ShouldBeTrue();
        }

        [Test]
        public void LookforAutowire_IMultipleAttributes1()
        {
            _container.ComponentRegistry.Registrations
                .Any(x =>
                    ContainsType(x.Services, typeof(IMultipleAttributes1))
                    && x.Activator.LimitType == typeof(MultipleAttributes)
                    && x.Lifetime is RootScopeLifetime)
                .ShouldBeTrue();
        }

        [Test]
        [Ignore("doesnt work in autofac, need to debug as to why")]
        public void LookforAutowire_IOpenGenericService()
        {
            _container.ComponentRegistry.Registrations
                .Any(x =>
                    ContainsType(x.Services, typeof(IOpenGenericService<>))
                    && x.Activator.LimitType == typeof(OpenGenericService<>)
                    && x.Lifetime is RootScopeLifetime)
                .ShouldBeTrue();
        }

        [Test]
        public void LookforAutowire_IClosedGenericService()
        {
            _container.ComponentRegistry.Registrations
                .Any(x =>
                    ContainsType(x.Services, typeof(IClosedGenericService<List<int>>))
                    && x.Activator.LimitType == typeof(ClosedGenericService<List<int>>)
                    && x.Lifetime is RootScopeLifetime)
                .ShouldBeTrue();
        }

        [Test]
        public void LookforAutowire_GenericWithFactory()
        {
            _container.ComponentRegistry.Registrations
                .Any(x =>
                    ContainsType(x.Services, typeof(IGenericWithFactory<ArrayList>))
                    && x.Activator.LimitType != null
                    && x.Lifetime is CurrentScopeLifetime)
                .ShouldBeTrue();
            _container.ComponentRegistry.Registrations
                .Any(x =>
                    ContainsType(x.Services, typeof(IGenericWithFactory<Hashtable>))
                    && x.Activator.LimitType != null
                    && x.Lifetime is CurrentScopeLifetime)
                .ShouldBeTrue();
            _container.ComponentRegistry.Registrations
                .Any(x =>
                    ContainsType(x.Services, typeof(IGenericWithFactory<Stack>))
                    && x.Activator.LimitType != null
                    && x.Lifetime is CurrentScopeLifetime)
                .ShouldBeTrue();
        }
    }
}
