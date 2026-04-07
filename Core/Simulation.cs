namespace FisheryMAUI.Core;

public sealed class SimulationScenario
{
    public SimulationScenario(string name, DailyTemperatureSeries temperatures)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Нужно указать имя сценария.", nameof(name));
        }

        ArgumentNullException.ThrowIfNull(temperatures);

        Name = name;
        Temperatures = temperatures;
    }

    public string Name { get; }

    public DailyTemperatureSeries Temperatures { get; }
}

public sealed class SimulationResult
{
    public SimulationResult(
        SimulationScenario scenario,
        int spawningDay,
        double incubationDays,
        double averageTemperature,
        double larvaeDay)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        Scenario = scenario;
        SpawningDay = spawningDay;
        IncubationDays = incubationDays;
        AverageTemperature = averageTemperature;
        LarvaeDay = larvaeDay;
    }

    public SimulationScenario Scenario { get; }

    public int SpawningDay { get; }

    public double IncubationDays { get; }

    public double AverageTemperature { get; }

    public double LarvaeDay { get; }
}

public sealed class DailyTemperatureCurveBuilder
{
    private const int DaysInYear = 365;

    private static readonly int[] MonthDays = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

    public DailyTemperatureSeries Build(MonthlyTemperatureProfile monthlyProfile)
    {
        ArgumentNullException.ThrowIfNull(monthlyProfile);

        double[] dailyValues = new double[DaysInYear];
        List<int> centers = BuildMonthCenters();

        for (int day = 0; day < DaysInYear; day++)
        {
            dailyValues[day] = InterpolateMonthly(day, centers, monthlyProfile.Values);
        }

        return new DailyTemperatureSeries(dailyValues);
    }

    private static List<int> BuildMonthCenters()
    {
        var centers = new List<int>();
        int cumulative = 0;

        for (int month = 0; month < MonthDays.Length; month++)
        {
            centers.Add(cumulative + MonthDays[month] / 2);
            cumulative += MonthDays[month];
        }

        return centers;
    }

    private static double InterpolateMonthly(int day, IReadOnlyList<int> centers, IReadOnlyList<double> monthlyTemperatures)
    {
        if (day <= centers[0])
        {
            int prevCenter = centers[11] - DaysInYear;
            int nextCenter = centers[0];
            return LinearInterpolation(day, prevCenter, monthlyTemperatures[11], nextCenter, monthlyTemperatures[0]);
        }

        if (day >= centers[11])
        {
            int prevCenter = centers[11];
            int nextCenter = centers[0] + DaysInYear;
            return LinearInterpolation(day, prevCenter, monthlyTemperatures[11], nextCenter, monthlyTemperatures[0]);
        }

        for (int index = 0; index < centers.Count - 1; index++)
        {
            if (day >= centers[index] && day <= centers[index + 1])
            {
                return LinearInterpolation(
                    day,
                    centers[index],
                    monthlyTemperatures[index],
                    centers[index + 1],
                    monthlyTemperatures[index + 1]);
            }
        }

        return monthlyTemperatures[0];
    }

    private static double LinearInterpolation(double x, double x1, double y1, double x2, double y2)
    {
        if (Math.Abs(x2 - x1) < 1e-12)
        {
            return y1;
        }

        return y1 + (y2 - y1) * (x - x1) / (x2 - x1);
    }
}

public sealed class ClimateScenarioFactory
{
    public IReadOnlyList<SimulationScenario> CreateStandardSet(FishSpecies species, DailyTemperatureSeries averageYear)
    {
        ArgumentNullException.ThrowIfNull(species);
        ArgumentNullException.ThrowIfNull(averageYear);

        return
        [
            new SimulationScenario("Средний год", averageYear),
            new SimulationScenario("Теплый год", averageYear.Shift(species.Anomaly)),
            new SimulationScenario("Холодный год", averageYear.Shift(-species.Anomaly))
        ];
    }
}

public sealed class SpawningSimulationEngine
{
    private const int RequiredWarmerDaysAfterSpawning = 45;

    private readonly double _convergenceEpsilon;
    private readonly int _maxIterations;

    public SpawningSimulationEngine(double convergenceEpsilon = 0.01, int maxIterations = 1000)
    {
        _convergenceEpsilon = convergenceEpsilon;
        _maxIterations = maxIterations;
    }

    public SimulationResult Run(FishSpecies species, SimulationScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(species);
        ArgumentNullException.ThrowIfNull(scenario);

        int spawningDay = scenario.Temperatures.FindFirstSpawningDay(
            species.SpawningTemperature,
            RequiredWarmerDaysAfterSpawning,
            species.SpawningPeriod.StartDayOfYear,
            species.SpawningPeriod.EndDayOfYear);

        if (spawningDay == -1 && species.SpawningPeriod.EndDayOfYear < scenario.Temperatures.DayCount)
        {
            spawningDay = scenario.Temperatures.FindFirstSpawningDay(
                species.SpawningTemperature,
                RequiredWarmerDaysAfterSpawning,
                species.SpawningPeriod.EndDayOfYear + 1,
                scenario.Temperatures.DayCount);
        }

        if (spawningDay == -1)
        {
            throw new InvalidOperationException(
                $"В сценарии '{scenario.Name}' не найден день нереста: сначала поиск выполняется в биологическом периоде вида, затем после его конца, но условие порога и {RequiredWarmerDaysAfterSpawning} последующих суток выше порога нигде не выполнено.");
        }

        // Первое приближение считаем по температуре нереста.
        double incubationDays = species.CalculateIncubationDays(species.SpawningTemperature);
        double averageTemperature = 0.0;

        for (int iteration = 0; iteration < _maxIterations; iteration++)
        {
            averageTemperature = CalculateAverageTemperature(scenario.Temperatures.Values, spawningDay, incubationDays);
            double nextIncubationDays = species.CalculateIncubationDays(averageTemperature);

            if (Math.Abs(nextIncubationDays - incubationDays) < _convergenceEpsilon)
            {
                incubationDays = nextIncubationDays;
                break;
            }

            incubationDays = nextIncubationDays;
        }

        return new SimulationResult(
            scenario,
            spawningDay,
            incubationDays,
            averageTemperature,
            spawningDay + incubationDays);
    }

    private static double CalculateAverageTemperature(IReadOnlyList<double> dailyTemps, int startDay1Based, double durationDays)
    {
        double startIndex = startDay1Based - 1;
        double endIndex = startIndex + durationDays;

        int wholeStart = (int)Math.Floor(startIndex);
        int wholeEnd = (int)Math.Floor(endIndex);

        double sum = 0.0;
        double totalWeight = 0.0;

        for (int index = wholeStart; index <= wholeEnd; index++)
        {
            if (index < 0 || index >= dailyTemps.Count)
            {
                break;
            }

            double left = Math.Max(index, startIndex);
            double right = Math.Min(index + 1.0, endIndex);
            double weight = right - left;

            if (weight > 0)
            {
                sum += dailyTemps[index] * weight;
                totalWeight += weight;
            }
        }

        if (totalWeight == 0)
        {
            throw new InvalidOperationException("Невозможно вычислить среднюю температуру.");
        }

        return sum / totalWeight;
    }
}
