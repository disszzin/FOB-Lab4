using FisheryMAUI.Core;
using ScottPlot;

namespace FisheryMAUI.Plotting;

public sealed class TemperatureOverviewGraphBuilder : IGraphBuilder
{
    public string Id => "overview";

    public string Title => "Общий график";

    public string Description => "Все три температурных сценария на одном графике с общей линией порога нереста.";

    public void Build(GraphContext context, Plot plot)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(plot);

        plot.Clear();

        double[] xs = Enumerable.Range(1, context.Scenarios[0].Temperatures.DayCount)
            .Select(day => (double)day)
            .ToArray();

        ScottPlot.Color[] palette =
        [
            ScottPlot.Colors.Teal,
            ScottPlot.Colors.Orange,
            ScottPlot.Colors.DodgerBlue
        ];

        for (int index = 0; index < context.Scenarios.Count; index++)
        {
            SimulationScenario scenario = context.Scenarios[index];
            var scatter = plot.Add.ScatterLine(xs, scenario.Temperatures.Values.ToArray());
            scatter.LegendText = scenario.Name;
            scatter.Color = palette[index % palette.Length];
            scatter.LineWidth = 2.5F;
        }

        var threshold = plot.Add.HorizontalLine(context.Species.SpawningTemperature);
        threshold.LegendText = $"Порог нереста: {context.Species.SpawningTemperature:F1} °C";
        threshold.Color = ScottPlot.Colors.Black;
        threshold.LineWidth = 1.5F;

        plot.Title("Общий температурный график");
        plot.XLabel("Дата");
        plot.YLabel("Температура воды, °C");
        GraphStyle.ApplyOverviewTicks(plot);
        plot.Axes.AutoScale();
        plot.Axes.Margins(0.02, 0.12);

        GraphStyle.PlaceLegendOutside(plot);
    }
}

public sealed class ScenarioTemperatureGraphBuilder : IGraphBuilder
{
    private readonly int _scenarioIndex;
    private readonly ScottPlot.Color _accentColor;

    public ScenarioTemperatureGraphBuilder(string id, string title, string description, int scenarioIndex, ScottPlot.Color accentColor)
    {
        Id = id;
        Title = title;
        Description = description;
        _scenarioIndex = scenarioIndex;
        _accentColor = accentColor;
    }

    public string Id { get; }

    public string Title { get; }

    public string Description { get; }

    public void Build(GraphContext context, Plot plot)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(plot);

        plot.Clear();

        SimulationScenario scenario = context.Scenarios[_scenarioIndex];
        SimulationResult result = context.Results[_scenarioIndex];

        double[] xs = Enumerable.Range(1, scenario.Temperatures.DayCount)
            .Select(day => (double)day)
            .ToArray();
        double[] ys = scenario.Temperatures.Values.ToArray();

        double baselineY = Math.Min(context.Species.SpawningTemperature, ys.Min()) - 1.0;
        double spawnX = result.SpawningDay;
        double spawnY = GraphStyle.GetTemperatureAtDay(scenario.Temperatures, spawnX);
        double larvaeX = result.LarvaeDay;
        double larvaeY = GraphStyle.GetTemperatureAtDay(scenario.Temperatures, larvaeX);
        string spawnLegendText = GraphStyle.CreateMarkerLegendText("Дн", spawnX, spawnY);
        string larvaeLegendText = GraphStyle.CreateMarkerLegendText("Дл", larvaeX, larvaeY);

        var temperatureCurve = plot.Add.ScatterLine(xs, ys);
        temperatureCurve.LegendText = "Температура воды";
        temperatureCurve.Color = _accentColor;
        temperatureCurve.LineWidth = 2.5F;

        var thresholdLine = plot.Add.HorizontalLine(context.Species.SpawningTemperature);
        thresholdLine.LegendText = $"Порог нереста: {context.Species.SpawningTemperature:F1} °C";
        thresholdLine.Color = ScottPlot.Colors.Black;
        thresholdLine.LineWidth = 1.5F;

        GraphStyle.AddGuideLine(plot, new[] { spawnX, spawnX }, new[] { baselineY, spawnY });
        GraphStyle.AddGuideLine(plot, new[] { larvaeX, larvaeX }, new[] { baselineY, larvaeY });
        GraphStyle.AddGuideLine(plot, new[] { 1.0, spawnX }, new[] { spawnY, spawnY });
        GraphStyle.AddGuideLine(plot, new[] { 1.0, larvaeX }, new[] { larvaeY, larvaeY });

        GraphStyle.AddGuideLabel(plot, "Дн", spawnX, baselineY);
        GraphStyle.AddGuideLabel(plot, "Дл", larvaeX, baselineY);
        GraphStyle.AddMarker(plot, spawnX, spawnY, ScottPlot.Colors.ForestGreen, spawnLegendText);
        GraphStyle.AddMarker(plot, larvaeX, larvaeY, ScottPlot.Colors.OrangeRed, larvaeLegendText);

        plot.Title($"{scenario.Name}: температура и ключевые даты");
        plot.XLabel("Дата");
        plot.YLabel("Температура воды, °C");
        GraphStyle.ApplyOverviewTicks(plot);
        plot.Axes.AutoScale();
        plot.Axes.Margins(0.02, 0.22);

        GraphStyle.PlaceLegendOutside(plot);
    }
}

internal static class GraphStyle
{
    private static readonly int[] MonthDays = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

    public static void ApplyOverviewTicks(Plot plot)
    {
        ArgumentNullException.ThrowIfNull(plot);

        double[] positions = new double[MonthDays.Length];
        string[] labels = new string[MonthDays.Length];
        int day = 1;

        for (int month = 0; month < MonthDays.Length; month++)
        {
            positions[month] = day;
            labels[month] = $"01.{month + 1:00}";
            day += MonthDays[month];
        }

        plot.Axes.Bottom.SetTicks(positions, labels);
        plot.Axes.Bottom.TickLabelStyle.Rotation = 0;
        plot.Axes.Bottom.TickLabelStyle.FontSize = 9;
    }

    public static void MakeLegendCompact(Legend legend)
    {
        ArgumentNullException.ThrowIfNull(legend);

        legend.FontSize = 9;
        legend.Padding = new PixelPadding(4);
        legend.Margin = new PixelPadding(4);
        legend.SymbolWidth = 10;
        legend.SymbolHeight = 5;
        legend.SymbolPadding = 3;
        legend.InterItemPadding = new PixelPadding(3);
    }

    public static void PlaceLegendOutside(Plot plot)
    {
        ArgumentNullException.ThrowIfNull(plot);

        ScottPlot.Panels.LegendPanel[] existingLegendPanels = plot.Axes.GetPanels()
            .OfType<ScottPlot.Panels.LegendPanel>()
            .ToArray();

        foreach (ScottPlot.Panels.LegendPanel existingLegendPanel in existingLegendPanels)
        {
            plot.Axes.Remove(existingLegendPanel);
        }

        ScottPlot.Panels.LegendPanel legendPanel = plot.ShowLegend(Edge.Right);
        legendPanel.Alignment = Alignment.UpperCenter;
        legendPanel.Padding = new PixelPadding(6);
        legendPanel.MinimumSize = 96;

        MakeLegendCompact(legendPanel.Legend);
    }

    public static void AddGuideLine(Plot plot, double[] xs, double[] ys)
    {
        var guide = plot.Add.ScatterLine(xs, ys);
        guide.Color = ScottPlot.Colors.Gray;
        guide.LinePattern = LinePattern.Dashed;
        guide.LineWidth = 1.2F;
    }

    public static void AddGuideLabel(Plot plot, string text, double x, double y)
    {
        var label = plot.Add.Text(text, x, y);
        label.Alignment = Alignment.UpperCenter;
        label.LabelFontSize = 9;
        label.LabelFontColor = ScottPlot.Colors.Gray;
        label.LabelBackgroundColor = ScottPlot.Colors.White;
        label.LabelBorderWidth = 0;
        label.LabelPadding = 2;
    }

    public static void AddMarker(Plot plot, double x, double y, ScottPlot.Color color, string legendText)
    {
        var marker = plot.Add.ScatterLine(new[] { x }, new[] { y });
        marker.Color = color;
        marker.LegendText = legendText;
        marker.LineWidth = 0;
        marker.MarkerShape = MarkerShape.FilledCircle;
        marker.MarkerSize = 9;
    }

    public static string CreateMarkerLegendText(string markerName, double dayOfYear, double temperature)
    {
        return $"{markerName}: {CreateDateLabel(dayOfYear)} · {temperature:F2} °C";
    }

    public static double GetTemperatureAtDay(DailyTemperatureSeries temperatures, double dayOfYear)
    {
        double index = Math.Clamp(dayOfYear - 1.0, 0, temperatures.DayCount - 1);
        int leftIndex = (int)Math.Floor(index);
        int rightIndex = Math.Min(leftIndex + 1, temperatures.DayCount - 1);

        if (leftIndex == rightIndex)
        {
            return temperatures[leftIndex];
        }

        double fraction = index - leftIndex;
        return temperatures[leftIndex] + (temperatures[rightIndex] - temperatures[leftIndex]) * fraction;
    }

    public static string CreateDateLabel(double dayOfYear)
    {
        (int day, int month) = GetDayAndMonth((int)Math.Round(dayOfYear));
        return $"{day:00}.{month:00}";
    }

    private static (int Day, int Month) GetDayAndMonth(int dayOfYear)
    {
        int day = Math.Clamp(dayOfYear, 1, 365);
        int month = 1;

        for (int index = 0; index < MonthDays.Length; index++)
        {
            if (day <= MonthDays[index])
            {
                return (day, month);
            }

            day -= MonthDays[index];
            month++;
        }

        return (31, 12);
    }
}
