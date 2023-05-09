using Agoda.IoC.Generator.UnitTests.Helpers;

namespace Agoda.IoC.Generator.UnitTests;

[TestFixture]
public class ContainerRegistrationGenerator
{

    private static IEnumerable<TestCaseData> ContainerRegistrationGeneratorTestDatas()
    {
        yield return new TestCaseData(@"
using using Agoda.IoC.Generator.Abstractions;
namespace Agoda.IoC.Generator.UnitTests;

[RegisterHostedService]
public class TimedHostedService : IHostedService
{
    public TimedHostedService() { }
    public Task StartAsync(CancellationToken stoppingToken) => Task.CompletedTask;    
    public Task StopAsync(CancellationToken stoppingToken)=>  Task.CompletedTask;
}
", @"serviceCollection.AddHostedService<TimedHostedService>();
return serviceCollection;");
    }

    [Test, TestCaseSource("ContainerRegistrationGeneratorTestDatas")]
    public void Should_Generate_AddHostedService_Correctly(string source, string generatedBodyMethod)
    {
        TestHelper.GenerateAgodaIoC(source)
                .Should()
                .HaveMethodCount(2)
                .HaveMethods("Register", "RegisterFromAgodaIoCGeneratorUnitTests")
                .HaveMethodBody("Register", generatedBodyMethod);
    }
}
