namespace FisheryMAUI.Core;

public sealed class YearCalendar
{
    private static readonly int[] MonthDays = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

    private static readonly string[] MonthNamesGenitive =
    {
        "января", "февраля", "марта", "апреля", "мая", "июня",
        "июля", "августа", "сентября", "октября", "ноября", "декабря"
    };

    public string ToDateString(double dayOfYear)
    {
        int day = (int)Math.Floor(dayOfYear);
        day = Math.Clamp(day, 1, 365);

        int month = 0;
        while (day > MonthDays[month])
        {
            day -= MonthDays[month];
            month++;
        }

        return $"{day} {MonthNamesGenitive[month]}";
    }
}

public sealed class CalculationResultFormatter
{
    private readonly YearCalendar _calendar;

    public CalculationResultFormatter(YearCalendar calendar)
    {
        _calendar = calendar ?? throw new ArgumentNullException(nameof(calendar));
    }

    public string Format(SimulationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return
            $"Сценарий: {result.Scenario.Name}\n" +
            $"  День начала нереста: {result.SpawningDay} ({_calendar.ToDateString(result.SpawningDay)})\n" +
            $"  Средняя температура за инкубацию: {result.AverageTemperature:F2} °C\n" +
            $"  Длительность инкубации: {result.IncubationDays:F2} суток\n" +
            $"  День появления личинок: {result.LarvaeDay:F2} ({_calendar.ToDateString(result.LarvaeDay)})";
    }
}

public sealed class ScenarioSummary
{
    public ScenarioSummary(
        string name,
        string accentHex,
        string spawningDate,
        string larvaeDate,
        double averageTemperature,
        double incubationDays,
        int spawningDay)
    {
        Name = name;
        AccentHex = accentHex;
        SpawningDate = spawningDate;
        LarvaeDate = larvaeDate;
        AverageTemperature = averageTemperature;
        IncubationDays = incubationDays;
        SpawningDay = spawningDay;
    }

    public string Name { get; }

    public string AccentHex { get; }

    public string SpawningDate { get; }

    public string LarvaeDate { get; }

    public double AverageTemperature { get; }

    public double IncubationDays { get; }

    public int SpawningDay { get; }
}
