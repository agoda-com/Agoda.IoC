using Agoda.IoC.Generator.Analyzers;
using Agoda.IoC.Generator.UnitTests.Helpers;
using FluentAssertions;

namespace Agoda.IoC.Generator.UnitTests;

[TestFixture]
public class IoCGraphAnalyzerUnitTests
{
    [Test]
    public async Task Analyze_WhenConstructorDependencyIsNotRegistered_ReportsMissingRegistration()
    {
        const string source = @"
using Agoda.IoC.Generator.Abstractions;

[RegisterSingleton]
public class WidgetService
{
    public WidgetService(IWidgetPricingClient pricingClient) { }
}

public interface IWidgetPricingClient { }
";

        var diagnostics = await AnalyzerTestHelper.GetDiagnostics(source);

        diagnostics.Should().ContainSingle(diagnostic => diagnostic.Id == IoCGraphAnalyzer.MissingRegistration.Id);
    }

    [Test]
    public async Task Analyze_WhenConstructorDependencyIsExternallyProvided_DoesNotReportMissingRegistration()
    {
        const string source = @"
using Agoda.IoC.Generator.Abstractions;

[assembly: ExternallyProvided(typeof(IWidgetPricingClient))]

[RegisterSingleton]
public class WidgetService
{
    public WidgetService(IWidgetPricingClient pricingClient) { }
}

public interface IWidgetPricingClient { }
";

        var diagnostics = await AnalyzerTestHelper.GetDiagnostics(source);

        diagnostics.Should().NotContain(diagnostic => diagnostic.Id == IoCGraphAnalyzer.MissingRegistration.Id);
    }

    [Test]
    public async Task Analyze_WhenConstructorDependencyIsRegisteredInReferencedAssembly_DoesNotReportMissingRegistration()
    {
        const string referencedSource = @"
using Agoda.IoC.Generator.Abstractions;

namespace ReferencedServices;

public interface IWidgetPricingClient { }

[RegisterScoped]
public class WidgetPricingClient : IWidgetPricingClient { }
";

        var referencedAssembly = AnalyzerTestHelper.CreateReference(referencedSource, "ReferencedServices");

        const string source = @"
using Agoda.IoC.Generator.Abstractions;
using ReferencedServices;

[RegisterTransient]
public class WidgetService
{
    public WidgetService(IWidgetPricingClient pricingClient) { }
}
";

        var diagnostics = await AnalyzerTestHelper.GetDiagnostics(source, referencedAssembly);

        diagnostics.Should().NotContain(diagnostic => diagnostic.Id == IoCGraphAnalyzer.MissingRegistration.Id);
    }

    [Test]
    public async Task Analyze_WhenSingletonDependsOnScopedService_ReportsCaptiveDependency()
    {
        const string source = @"
using Agoda.IoC.Generator.Abstractions;

[RegisterScoped]
public class RequestContext : IRequestContext { }

public interface IRequestContext { }

[RegisterSingleton]
public class WidgetService
{
    public WidgetService(IRequestContext requestContext) { }
}
";

        var diagnostics = await AnalyzerTestHelper.GetDiagnostics(source);

        diagnostics.Should().ContainSingle(diagnostic => diagnostic.Id == IoCGraphAnalyzer.CaptiveDependency.Id);
    }

    [Test]
    public async Task Analyze_WhenServiceHasMultipleNonCollectionRegistrations_ReportsDuplicateRegistrations()
    {
        const string source = @"
using Agoda.IoC.Generator.Abstractions;

public interface IWidgetService { }

[RegisterSingleton]
public class FirstWidgetService : IWidgetService { }

[RegisterSingleton]
public class SecondWidgetService : IWidgetService { }
";

        var diagnostics = await AnalyzerTestHelper.GetDiagnostics(source);

        diagnostics.Where(diagnostic => diagnostic.Id == IoCGraphAnalyzer.DuplicateRegistration.Id)
            .Should()
            .HaveCount(2);
    }

    [Test]
    public async Task Analyze_WhenDuplicateServiceUsesReplaceService_DoesNotReportDuplicateRegistration()
    {
        const string source = @"
using Agoda.IoC.Generator.Abstractions;

public interface IWidgetService { }

[RegisterSingleton]
public class FirstWidgetService : IWidgetService { }

[RegisterSingleton(ReplaceService = true)]
public class SecondWidgetService : IWidgetService { }
";

        var diagnostics = await AnalyzerTestHelper.GetDiagnostics(source);

        diagnostics.Should().NotContain(diagnostic => diagnostic.Id == IoCGraphAnalyzer.DuplicateRegistration.Id);
    }

    [Test]
    public async Task Analyze_WhenFactoryDoesNotBuildRegisteredService_ReportsInvalidFactory()
    {
        const string source = @"
using System;
using Agoda.IoC.Generator.Abstractions;

public interface IWidgetService { }

[RegisterSingleton(For = typeof(IWidgetService), Factory = typeof(WidgetFactory))]
public class WidgetService : IWidgetService { }

public class OtherWidgetService { }

public class WidgetFactory : IImplementationFactory<OtherWidgetService>
{
    public OtherWidgetService Factory(IServiceProvider serviceProvider) => new OtherWidgetService();
}
";

        var diagnostics = await AnalyzerTestHelper.GetDiagnostics(source);

        diagnostics.Should().ContainSingle(diagnostic => diagnostic.Id == IoCGraphAnalyzer.InvalidFactory.Id);
    }

    [Test]
    public async Task Analyze_WhenForTypeIsNotImplemented_ReportsAttributeMisuse()
    {
        const string source = @"
using Agoda.IoC.Generator.Abstractions;

public interface IWidgetService { }

[RegisterSingleton(For = typeof(IWidgetService))]
public class WidgetService { }
";

        var diagnostics = await AnalyzerTestHelper.GetDiagnostics(source);

        diagnostics.Should().ContainSingle(diagnostic => diagnostic.Id == IoCGraphAnalyzer.AttributeMisuse.Id);
    }

    [Test]
    public async Task Analyze_WhenCollectionOrderIsDuplicated_ReportsAttributeMisuse()
    {
        const string source = @"
using Agoda.IoC.Generator.Abstractions;

public interface IPipeline { }

[RegisterSingleton(For = typeof(IPipeline), OfCollection = true, Order = 1)]
public class FirstPipeline : IPipeline { }

[RegisterSingleton(For = typeof(IPipeline), OfCollection = true, Order = 1)]
public class SecondPipeline : IPipeline { }
";

        var diagnostics = await AnalyzerTestHelper.GetDiagnostics(source);

        diagnostics.Where(diagnostic => diagnostic.Id == IoCGraphAnalyzer.AttributeMisuse.Id)
            .Should()
            .HaveCount(2);
    }

    [Test]
    public async Task Analyze_WhenRegisteredConstructorsAreCircular_ReportsCircularDependency()
    {
        const string source = @"
using Agoda.IoC.Generator.Abstractions;

public interface IClassA { }
public interface IClassB { }

[RegisterSingleton]
public class ClassA : IClassA
{
    public ClassA(IClassB classB) { }
}

[RegisterSingleton]
public class ClassB : IClassB
{
    public ClassB(IClassA classA) { }
}
";

        var diagnostics = await AnalyzerTestHelper.GetDiagnostics(source);

        diagnostics.Should().ContainSingle(diagnostic => diagnostic.Id == IoCGraphAnalyzer.CircularDependency.Id);
    }
}
