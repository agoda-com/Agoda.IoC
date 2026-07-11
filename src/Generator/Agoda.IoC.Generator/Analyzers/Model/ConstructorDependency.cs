using Microsoft.CodeAnalysis;

namespace Agoda.IoC.Generator.Analyzers;

internal sealed class ConstructorDependency
{
    public ConstructorDependency(IParameterSymbol parameter, IReadOnlyList<Registration> registrations)
    {
        Parameter = parameter;
        Registrations = registrations;
    }

    public IParameterSymbol Parameter { get; }

    public IReadOnlyList<Registration> Registrations { get; }
}
