using System;
using Agoda.IoC.Core;
using Agoda.IoC.ProjectUnderTest.Invalid12;
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
            Assert.Throws<RegistrationFailedException>(() => container.AutoWireAssembly(new[]
            {
                typeof(AmbiguousRegistration).Assembly
            }, false, options =>
            {
                options.OnRegistrationContextException = exception => { };
            }));
            try
            {
                container.AutoWireAssembly(new[]
                {
                    typeof(AmbiguousRegistration).Assembly
                }, false);
            }
            catch (RegistrationFailedException registrationFailedException)
            {
                registrationFailedException.RegistrationContextExceptions.Count.ShouldBe(1);
                registrationFailedException.RegistrationContextExceptions[0].Message.ShouldContain("AmbiguousRegistration");
            }
        }
    }
}
