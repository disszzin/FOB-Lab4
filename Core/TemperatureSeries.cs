namespace FisheryMAUI.Core;

public sealed class MonthlyTemperatureProfile
{
    private readonly double[] _values;

    public MonthlyTemperatureProfile(IEnumerable<double> monthlyTemperatures)
    {
        ArgumentNullException.ThrowIfNull(monthlyTemperatures);

        _values = monthlyTemperatures.ToArray();

        if (_values.Length != 12)
        {
            throw new ArgumentException("Нужно задать 12 среднемесячных температур.", nameof(monthlyTemperatures));
        }
    }

    public IReadOnlyList<double> Values => _values;

    public double this[int monthIndexZeroBased] => _values[monthIndexZeroBased];
}

public sealed class DailyTemperatureSeries
{
    private readonly double[] _values;

    public DailyTemperatureSeries(IEnumerable<double> temperatures)
    {
        ArgumentNullException.ThrowIfNull(temperatures);

        _values = temperatures.ToArray();

        if (_values.Length == 0)
        {
            throw new ArgumentException("Серия температур не может быть пустой.", nameof(temperatures));
        }
    }

    public int DayCount => _values.Length;

    public IReadOnlyList<double> Values => _values;

    public double this[int dayIndexZeroBased] => _values[dayIndexZeroBased];

    public DailyTemperatureSeries Shift(double delta)
    {
        return new DailyTemperatureSeries(_values.Select(value => value + delta));
    }

    public int FindFirstDayAtOrAbove(double threshold)
    {
        for (int day = 0; day < _values.Length; day++)
        {
            if (_values[day] >= threshold)
            {
                return day + 1;
            }
        }

        return -1;
    }

    public int FindFirstSpawningDay(double threshold, int requiredWarmerDays)
    {
        return FindFirstSpawningDay(threshold, requiredWarmerDays, 1, _values.Length);
    }

    public int FindFirstSpawningDay(double threshold, int requiredWarmerDays, int startDay1Based, int endDay1Based)
    {
        if (requiredWarmerDays < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(requiredWarmerDays));
        }

        if (startDay1Based < 1 || startDay1Based > _values.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(startDay1Based));
        }

        if (endDay1Based < startDay1Based || endDay1Based > _values.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(endDay1Based));
        }

        int startIndex = startDay1Based - 1;
        int endIndex = endDay1Based - 1;

        for (int day = startIndex; day <= endIndex; day++)
        {
            if (_values[day] < threshold)
            {
                continue;
            }

            if (day + requiredWarmerDays >= _values.Length)
            {
                return -1;
            }

            bool hasEnoughWarmerDays = true;

            for (int nextDay = day + 1; nextDay <= day + requiredWarmerDays; nextDay++)
            {
                if (_values[nextDay] <= threshold)
                {
                    hasEnoughWarmerDays = false;
                    break;
                }
            }

            if (hasEnoughWarmerDays)
            {
                return day + 1;
            }
        }

        return -1;
    }
}
