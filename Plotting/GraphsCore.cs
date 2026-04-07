using FisheryMAUI.Core;

namespace FisheryMAUI.Plotting;

public sealed class GraphContext
{
    public GraphContext(
        FishSpecies species,
        MonthlyTemperatureProfile temperatureProfile,
        IReadOnlyList<SimulationScenario> scenarios,
        IReadOnlyList<SimulationResult> results)
    {
        Species = species ?? throw new ArgumentNullException(nameof(species));
        TemperatureProfile = temperatureProfile ?? throw new ArgumentNullException(nameof(temperatureProfile));
        Scenarios = scenarios ?? throw new ArgumentNullException(nameof(scenarios));
        Results = results ?? throw new ArgumentNullException(nameof(results));
    }

    public FishSpecies Species { get; }

    public MonthlyTemperatureProfile TemperatureProfile { get; }

    public IReadOnlyList<SimulationScenario> Scenarios { get; }

    public IReadOnlyList<SimulationResult> Results { get; }
}

public interface IGraphBuilder
{
    string Id { get; }

    string Title { get; }

    string Description { get; }

    void Build(GraphContext context, ScottPlot.Plot plot);
}

public sealed class GraphRegistry
{
    private readonly Dictionary<string, IGraphBuilder> _builders = new(StringComparer.OrdinalIgnoreCase);

    public void Register(IGraphBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (!_builders.TryAdd(builder.Id, builder))
        {
            throw new InvalidOperationException($"График с идентификатором '{builder.Id}' уже зарегистрирован.");
        }
    }

    public IGraphBuilder Resolve(string graphId)
    {
        if (!_builders.TryGetValue(graphId, out IGraphBuilder? builder))
        {
            throw new KeyNotFoundException($"График '{graphId}' не зарегистрирован.");
        }

        return builder;
    }
}

public sealed class GraphDashboard
{
    private readonly GraphRegistry _registry;
    private readonly List<string> _assignedGraphIds = new();

    public GraphDashboard(GraphRegistry registry)
    {
        _registry = registry;
    }

    public void Assign(string graphId)
    {
        _assignedGraphIds.Add(graphId);
    }

    public IReadOnlyList<IGraphBuilder> ResolveAssignedGraphs()
    {
        return _assignedGraphIds.Select(_registry.Resolve).ToArray();
    }
}
