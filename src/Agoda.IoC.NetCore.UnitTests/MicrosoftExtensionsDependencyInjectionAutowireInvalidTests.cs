using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Agoda.IoC.Core;
using Agoda.IoC.ProjectUnderTest.Invalid12;
using Agoda.IoC.ProjectUnderTest.Valid;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace Agoda.IoC.NetCore.UnitTests
{
    [TestFixture]
    public class MicrosoftExtensionsDependencyInjectionAutowireInvalidTests
    {
        [Test]
        public void RegisterByAttribute_WithAmbiguousRegistration_Throws()
        {
            var container = new ServiceCollection();
            Assert.Throws<ContainerRegistrationContextException>(() => container.AutoWireAssembly(new[]
            {
                typeof(AmbiguousRegistration).Assembly
            }, false,onexceptio => {
                onexceptio.Ignore = false;
            }));
        }
    }
}
