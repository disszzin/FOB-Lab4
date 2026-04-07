using FisheryMAUI.Plotting;

namespace FisheryMAUI.Core;

public sealed class SimulationInput
{
    public SimulationInput(FishType fishType, double spawningTemperature, double anomaly, IReadOnlyList<double> monthlyTemperatures)
    {
        FishType = fishType;
        SpawningTemperature = spawningTemperature;
        Anomaly = anomaly;
        MonthlyTemperatures = monthlyTemperatures;
    }

    public FishType FishType { get; }

    public double SpawningTemperature { get; }

    public double Anomaly { get; }

    public IReadOnlyList<double> MonthlyTemperatures { get; }

    public static SimulationInput CreateDefault()
    {
        return new SimulationInput(
            FishType.Cod,
            spawningTemperature: 5.5,
            anomaly: 1.0,
            monthlyTemperatures:
            [
                5.780100, 5.518800, 5.217300, 5.255200, 6.199800, 8.033000,
                10.60330, 11.15610, 10.15930, 8.501100, 7.349900, 6.477100
            ]);
    }
}

public sealed class SimulationDashboardModel
{
    public SimulationDashboardModel(
        GraphContext graphContext,
        IReadOnlyList<IGraphBuilder> assignedGraphs,
        IReadOnlyList<ScenarioSummary> scenarioSummaries,
        string summaryText)
    {
        GraphContext = graphContext;
        AssignedGraphs = assignedGraphs;
        ScenarioSummaries = scenarioSummaries;
        SummaryText = summaryText;
    }

    public GraphContext GraphContext { get; }

    public IReadOnlyList<IGraphBuilder> AssignedGraphs { get; }

    public IReadOnlyList<ScenarioSummary> ScenarioSummaries { get; }

    public string SummaryText { get; }
}

public static class SimulationDashboardFactory
{
    private static readonly string[] AccentPalette = ["#0F766E", "#D97706", "#2563EB"];

    public static SimulationDashboardModel Create(SimulationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        FishSpecies species = FishSpeciesCatalog.Create(
            input.FishType,
            input.SpawningTemperature,
            input.Anomaly);

        var monthlyTemperatures = new MonthlyTemperatureProfile(input.MonthlyTemperatures);
        var curveBuilder = new DailyTemperatureCurveBuilder();
        DailyTemperatureSeries averageYear = curveBuilder.Build(monthlyTemperatures);

        var scenarioFactory = new ClimateScenarioFactory();
        IReadOnlyList<SimulationScenario> scenarios = scenarioFactory.CreateStandardSet(species, averageYear);

        var simulationEngine = new SpawningSimulationEngine();
        SimulationResult[] results = scenarios
            .Select(scenario => simulationEngine.Run(species, scenario))
            .ToArray();

        var calendar = new YearCalendar();
        var formatter = new CalculationResultFormatter(calendar);

        ScenarioSummary[] summaries = results
            .Select((result, index) => new ScenarioSummary(
                result.Scenario.Name,
                AccentPalette[index % AccentPalette.Length],
                calendar.ToDateString(result.SpawningDay),
                calendar.ToDateString(result.LarvaeDay),
                result.AverageTemperature,
                result.IncubationDays,
                result.SpawningDay))
            .ToArray();

        string summaryText = string.Join(
            $"{Environment.NewLine}{Environment.NewLine}",
            results.Select(result => formatter.Format(result)));

        var graphRegistry = new GraphRegistry();
        graphRegistry.Register(new TemperatureOverviewGraphBuilder());
        graphRegistry.Register(new ScenarioTemperatureGraphBuilder(
            id: "average-scenario",
            title: "Средний год",
            description: "Дневная температура, порог нереста и отметки ключевых моментов для среднего года.",
            scenarioIndex: 0,
            accentColor: ScottPlot.Colors.Teal));
        graphRegistry.Register(new ScenarioTemperatureGraphBuilder(
            id: "warm-scenario",
            title: "Теплый год",
            description: "Сценарий потепления с более ранним достижением порога нереста.",
            scenarioIndex: 1,
            accentColor: ScottPlot.Colors.Orange));
        graphRegistry.Register(new ScenarioTemperatureGraphBuilder(
            id: "cold-scenario",
            title: "Холодный год",
            description: "Сценарий похолодания с более медленным прогревом воды.",
            scenarioIndex: 2,
            accentColor: ScottPlot.Colors.DodgerBlue));

        var dashboard = new GraphDashboard(graphRegistry);
        dashboard.Assign("overview");
        dashboard.Assign("average-scenario");
        dashboard.Assign("warm-scenario");
        dashboard.Assign("cold-scenario");

        var graphContext = new GraphContext(species, monthlyTemperatures, scenarios, results);

        return new SimulationDashboardModel(graphContext, dashboard.ResolveAssignedGraphs(), summaries, summaryText);
    }
}
