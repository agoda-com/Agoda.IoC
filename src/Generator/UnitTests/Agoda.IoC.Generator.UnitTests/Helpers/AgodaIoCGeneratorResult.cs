using Microsoft.CodeAnalysis;

namespace Agoda.IoC.Generator.UnitTests.Helpers;
public record AgodaIoCGeneratorResult(
    IReadOnlyCollection<Diagnostic> Diagnostics,
    IReadOnlyDictionary<string, GeneratedMethod> Methods)
{
    public AgodaIoCGeneratorResultAssertions Should()
        => new(this);
}
