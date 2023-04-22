using FluentAssertions;
using Microsoft.CodeAnalysis;

namespace Agoda.IoC.Generator.UnitTests.Helpers;
public record DiagnosticMatcher(
    DiagnosticDescriptor Descriptor,
    string? Message = null)
{
    public bool Matches(Diagnostic diagnostic)
        => Descriptor.Equals(diagnostic.Descriptor);

    public void EnsureMatches(Diagnostic diagnostic)
    {
        diagnostic.Descriptor.Id
            .Should()
            .Be(Descriptor.Id);

        Message?.Should().Be(diagnostic.GetMessage(), $"Message for descriptor id {Descriptor.Id} does not match");
    }
}
