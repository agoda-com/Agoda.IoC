using Agoda.IoC.Core;
using Agoda.IoC.ProjectUnderTest.Valid;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace Agoda.IoC.NetCore.UnitTests
{
    [TestFixture]
    public class MicrosoftExtensionsDependencyInjectionAutowireTestsForKeyedFactory
    {
        private IServiceCollection _container;

        private IServiceCollection _containerMocked;

        [SetUp]
        public void SetUp()
        {
            _container = new ServiceCollection();
            _container.AutoWireAssembly(new[]
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
        public void LookforAutowire_IKeyedFactoryService()
        {
            var keyedFactoryService = _container.BuildServiceProvider().GetService<IKeyedComponentFactory<IKeyedFactoryService>>();

            var service1 = keyedFactoryService.GetByKey("Service_1");
            var service2 = keyedFactoryService.GetByKey("Service_2");

            service1.GetType().ShouldBe(typeof(KeyedFactoryService1));
            service2.GetType().ShouldBe(typeof(KeyedFactoryService2));

        }
    }
}