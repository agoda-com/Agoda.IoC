using Microsoft.CodeAnalysis;

namespace Agoda.IoC.Generator.Analyzers;

internal static class CircularDependencyRule
{
    internal static void Analyze(ServiceGraph graph, Action<Diagnostic> reportDiagnostic)
    {
        var registrationsByImplementation = graph.All
            .Where(registration => registration.FactoryType == null)
            .GroupBy(registration => registration.ImplementationType.GetTypeKey())
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var dependencyGraph = registrationsByImplementation.Values.ToDictionary(
            registration => registration.ImplementationType.GetTypeKey(),
            registration => graph.GetConstructorDependencies(registration)
                .SelectMany(dependency => dependency.Registrations)
                .Where(dependencyRegistration => dependencyRegistration.FactoryType == null)
                .Select(dependencyRegistration => dependencyRegistration.ImplementationType.GetTypeKey())
                .Where(registrationsByImplementation.ContainsKey)
                .Distinct()
                .ToList(),
            StringComparer.Ordinal);

        foreach (var cycle in new CycleDetector(dependencyGraph).FindCycles())
        {
            var reportRegistration = cycle
                .Take(cycle.Count - 1)
                .Select(key => registrationsByImplementation[key])
                .FirstOrDefault(registration => registration.IsFromCurrentCompilation);

            if (reportRegistration == null)
            {
                continue;
            }

            var cycleText = string.Join(
                " -> ",
                cycle.Select(key => registrationsByImplementation[key].ImplementationType.FormatType()));

            reportDiagnostic(Diagnostic.Create(Diagnostics.CircularDependency, reportRegistration.Location, cycleText));
        }
    }

    private sealed class CycleDetector
    {
        private readonly IReadOnlyDictionary<string, List<string>> _graph;
        private readonly List<string> _path = new();
        private readonly HashSet<string> _recursionStack = new(StringComparer.Ordinal);
        private readonly HashSet<string> _reportedCycles = new(StringComparer.Ordinal);
        private readonly HashSet<string> _visited = new(StringComparer.Ordinal);

        internal CycleDetector(IReadOnlyDictionary<string, List<string>> graph)
        {
            _graph = graph;
        }

        internal IEnumerable<IReadOnlyList<string>> FindCycles()
        {
            foreach (var node in _graph.Keys.OrderBy(key => key))
            {
                foreach (var cycle in Visit(node))
                {
                    yield return cycle;
                }
            }
        }

        private IEnumerable<IReadOnlyList<string>> Visit(string node)
        {
            if (_recursionStack.Contains(node))
            {
                yield break;
            }

            if (!_visited.Add(node))
            {
                yield break;
            }

            _recursionStack.Add(node);
            _path.Add(node);

            foreach (var dependency in _graph[node])
            {
                if (_recursionStack.Contains(dependency))
                {
                    var cycleStartIndex = _path.IndexOf(dependency);
                    var cycle = _path.Skip(cycleStartIndex).Concat(new[] { dependency }).ToList();
                    if (_reportedCycles.Add(GetCanonicalCycleKey(cycle)))
                    {
                        yield return cycle;
                    }

                    continue;
                }

                foreach (var cycle in Visit(dependency))
                {
                    yield return cycle;
                }
            }

            _path.RemoveAt(_path.Count - 1);
            _recursionStack.Remove(node);
        }

        private static string GetCanonicalCycleKey(IReadOnlyList<string> cycle)
        {
            return string.Join("|", cycle.Take(cycle.Count - 1).OrderBy(key => key));
        }
    }
}
