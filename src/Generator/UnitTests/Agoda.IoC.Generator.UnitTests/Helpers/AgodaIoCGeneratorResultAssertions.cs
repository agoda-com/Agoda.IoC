using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Microsoft.CodeAnalysis;

namespace Agoda.IoC.Generator.UnitTests.Helpers;

public class AgodaIoCGeneratorResultAssertions
{

    private readonly AgodaIoCGeneratorResult _agodaIoC;

    public AgodaIoCGeneratorResultAssertions(AgodaIoCGeneratorResult agodaIoC)
    {
        _agodaIoC = agodaIoC;
    }

    public AgodaIoCGeneratorResultAssertions HaveDiagnostics()
    {
        _agodaIoC.Diagnostics.Should().NotBeEmpty();
        return this;
    }

    public AgodaIoCGeneratorResultAssertions NotHaveDiagnostics(IReadOnlySet<DiagnosticSeverity> allowedDiagnosticSeverities)
    {
        _agodaIoC.Diagnostics
            .FirstOrDefault(d => !allowedDiagnosticSeverities.Contains(d.Severity))
            .Should()
            .BeNull();
        return this;
    }

    public AgodaIoCGeneratorResultAssertions HaveDiagnostic(DiagnosticMatcher diagnosticMatcher)
    {
        var diag = _agodaIoC.Diagnostics.FirstOrDefault(diagnosticMatcher.Matches);
        var foundIds = string.Join(", ", _agodaIoC.Diagnostics.Select(x => x.Descriptor.Id));
        diag.Should().NotBeNull($"No diagnostic with id {diagnosticMatcher.Descriptor.Id} found, found diagnostic ids: {foundIds}");
        diagnosticMatcher.EnsureMatches(diag!);
        return this;
    }

    public AgodaIoCGeneratorResultAssertions HaveSingleMethodBody(string registrationBody)
    {
        _agodaIoC.Methods.Single()
            .Value
            .Body
            .Should()
            .Be(registrationBody.ReplaceLineEndings());
        return this;
    }

    public AgodaIoCGeneratorResultAssertions HaveMethodCount(int count)
    {
        _agodaIoC.Methods.Should().HaveCount(count);
        return this;
    }

    public AgodaIoCGeneratorResultAssertions AllMethodsHaveBody(string registrationBody)
    {
        registrationBody = registrationBody.ReplaceLineEndings().Trim();
        foreach (var method in _agodaIoC.Methods.Values)
        {
            method.Body.Should().Be(registrationBody);
        }

        return this;
    }

    public AgodaIoCGeneratorResultAssertions HaveMethods(params string[] methodNames)
    {
        foreach (var methodName in methodNames)
        {
            _agodaIoC.Methods.Keys.Should().Contain(methodName);
        }

        return this;
    }

    public AgodaIoCGeneratorResultAssertions HaveOnlyMethods(params string[] methodNames)
    {
        HaveMethods(methodNames);
        HaveMethodCount(methodNames.Length);
        return this;
    }

    public AgodaIoCGeneratorResultAssertions HaveMethodBody(string methodName, string registrationBody)
    {
        _agodaIoC.Methods[methodName]
            .Body
            .Should()
            .Be(registrationBody.ReplaceLineEndings().Trim(), $"Method: {methodName}");
        return this;
    }

}
