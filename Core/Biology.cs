namespace FisheryMAUI.Core;

public enum FishType
{
    Herring,
    Cod,
    Sardine
}

public interface IIncubationFormula
{
    double Calculate(double averageTemperature);
}

public sealed class ExponentialIncubationFormula : IIncubationFormula
{
    private readonly double _baseDays;
    private readonly double _scale;
    private readonly double _temperatureFactor;

    public ExponentialIncubationFormula(double baseDays, double scale, double temperatureFactor)
    {
        _baseDays = baseDays;
        _scale = scale;
        _temperatureFactor = temperatureFactor;
    }

    public double Calculate(double averageTemperature)
    {
        return _baseDays + _scale * Math.Exp(_temperatureFactor * averageTemperature);
    }
}

public sealed class SpawningPeriod
{
    private static readonly int[] MonthDays = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

    public SpawningPeriod(int startMonth, int endMonth)
    {
        if (startMonth is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(startMonth));
        }

        if (endMonth is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(endMonth));
        }

        if (endMonth < startMonth)
        {
            throw new ArgumentException("Конец периода нереста не может быть раньше начала.", nameof(endMonth));
        }

        StartMonth = startMonth;
        EndMonth = endMonth;
    }

    public int StartMonth { get; }

    public int EndMonth { get; }

    public int StartDayOfYear => GetFirstDayOfMonth(StartMonth);

    public int EndDayOfYear => GetLastDayOfMonth(EndMonth);

    private static int GetFirstDayOfMonth(int month)
    {
        int day = 1;

        for (int index = 0; index < month - 1; index++)
        {
            day += MonthDays[index];
        }

        return day;
    }

    private static int GetLastDayOfMonth(int month)
    {
        int day = 0;

        for (int index = 0; index < month; index++)
        {
            day += MonthDays[index];
        }

        return day;
    }
}

public sealed class FishSpecies
{
    public FishSpecies(
        FishType type,
        string displayName,
        double spawningTemperature,
        double anomaly,
        IIncubationFormula incubationFormula,
        SpawningPeriod spawningPeriod)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Нужно указать отображаемое имя вида.", nameof(displayName));
        }

        ArgumentNullException.ThrowIfNull(incubationFormula);
        ArgumentNullException.ThrowIfNull(spawningPeriod);

        Type = type;
        DisplayName = displayName;
        SpawningTemperature = spawningTemperature;
        Anomaly = anomaly;
        IncubationFormula = incubationFormula;
        SpawningPeriod = spawningPeriod;
    }

    public FishType Type { get; }

    public string DisplayName { get; }

    public double SpawningTemperature { get; }

    public double Anomaly { get; }

    public IIncubationFormula IncubationFormula { get; }

    public SpawningPeriod SpawningPeriod { get; }

    public double CalculateIncubationDays(double averageTemperature)
    {
        return IncubationFormula.Calculate(averageTemperature);
    }
}

public static class FishSpeciesCatalog
{
    public static FishSpecies Create(FishType type, double spawningTemperature, double anomaly)
    {
        return new FishSpecies(
            type,
            GetDisplayName(type),
            spawningTemperature,
            anomaly,
            CreateFormula(type),
            GetSpawningPeriod(type));
    }

    public static string GetDisplayName(FishType type)
    {
        return type switch
        {
            FishType.Herring => "Сельдь",
            FishType.Cod => "Треска",
            FishType.Sardine => "Сардина",
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }

    private static IIncubationFormula CreateFormula(FishType type)
    {
        return type switch
        {
            FishType.Herring => new ExponentialIncubationFormula(4.0, 44.7, -0.167),
            FishType.Cod => new ExponentialIncubationFormula(7.0, 30.3, -0.215),
            FishType.Sardine => new ExponentialIncubationFormula(0.5, 28.8, -0.159),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }

    private static SpawningPeriod GetSpawningPeriod(FishType type)
    {
        return type switch
        {
            FishType.Cod => new SpawningPeriod(startMonth: 3, endMonth: 4),
            FishType.Herring => new SpawningPeriod(startMonth: 3, endMonth: 7),
            FishType.Sardine => new SpawningPeriod(startMonth: 1, endMonth: 3),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }
}
