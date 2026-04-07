using System.Globalization;
using FisheryMAUI.Core;
using FisheryMAUI.Plotting;
using Microsoft.Maui.Controls.Shapes;

namespace FisheryMAUI;

public partial class MainPage : ContentPage
{
    private static readonly string[] MonthLabels =
    [
        "Янв", "Фев", "Мар", "Апр", "Май", "Июн",
        "Июл", "Авг", "Сен", "Окт", "Ноя", "Дек"
    ];

    private readonly List<FishType> _fishTypes =
    [
        FishType.Cod,
        FishType.Herring,
        FishType.Sardine
    ];

    private readonly Dictionary<FishType, Button> _fishButtons = new();
    private readonly List<Entry> _monthlyEntries = [];
    private readonly List<Border> _monthlyBlocks = [];
    private readonly Dictionary<string, Button> _graphButtons = new(StringComparer.OrdinalIgnoreCase);
    private FishType _selectedFishType = FishType.Cod;
    private SimulationDashboardModel? _dashboard;
    private IGraphBuilder? _selectedGraph;

    public MainPage()
    {
        InitializeComponent();

        BuildFishButtons();
        BuildMonthlyInputs();

        SizeChanged += OnPageSizeChanged;

        ApplyInputToUi(SimulationInput.CreateDefault());
        RecalculateAndRender();
        UpdateResponsiveLayout();
    }

    private void BuildFishButtons()
    {
        FishButtonsGrid.Children.Clear();
        _fishButtons.Clear();

        for (int index = 0; index < _fishTypes.Count; index++)
        {
            FishType fishType = _fishTypes[index];
            var button = new Button
            {
                Text = FishSpeciesCatalog.GetDisplayName(fishType),
                CornerRadius = 18,
                HeightRequest = 48,
                FontFamily = "OpenSansSemibold",
                FontSize = 13,
                Padding = new Thickness(12, 10)
            };

            button.Clicked += async (_, _) =>
            {
                SelectFishType(fishType);

                if (_dashboard is not null)
                {
                    await RecalculateAndRenderAsync();
                }
            };

            _fishButtons[fishType] = button;
            Grid.SetColumn(button, index);
            FishButtonsGrid.Children.Add(button);
        }

        UpdateFishButtons();
    }

    private void BuildMonthlyInputs()
    {
        for (int index = 0; index < MonthLabels.Length; index++)
        {
            var entry = new Entry
            {
                Keyboard = Keyboard.Numeric,
                FontFamily = "OpenSansRegular",
                FontSize = 13,
                HorizontalTextAlignment = TextAlignment.Center,
                BackgroundColor = Colors.White,
                TextColor = Color.FromArgb("#24423B"),
                HeightRequest = 42,
                Margin = new Thickness(0, 2, 0, 0)
            };

            _monthlyEntries.Add(entry);

            var monthBlock = new Border
            {
                Padding = new Thickness(10, 9),
                BackgroundColor = Colors.White,
                Stroke = Color.FromArgb("#DCE7E0"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(14) },
                Content = new VerticalStackLayout
                {
                    Spacing = 4,
                    Children =
                    {
                        new Label
                        {
                            Text = MonthLabels[index],
                            FontFamily = "OpenSansSemibold",
                            FontSize = 12,
                            HorizontalTextAlignment = TextAlignment.Center,
                            TextColor = Color.FromArgb("#37544D")
                        },
                        entry
                    }
                }
            };

            _monthlyBlocks.Add(monthBlock);
            MonthlyInputsGrid.Children.Add(monthBlock);
        }
    }

    private void ApplyInputToUi(SimulationInput input)
    {
        _selectedFishType = input.FishType;
        UpdateFishButtons();

        SpawningTemperatureEntry.Text = input.SpawningTemperature.ToString("F1", CultureInfo.InvariantCulture);
        AnomalyEntry.Text = input.Anomaly.ToString("F1", CultureInfo.InvariantCulture);

        for (int i = 0; i < input.MonthlyTemperatures.Count; i++)
        {
            _monthlyEntries[i].Text = input.MonthlyTemperatures[i].ToString("F2", CultureInfo.InvariantCulture);
        }
    }

    private void SelectFishType(FishType fishType)
    {
        _selectedFishType = fishType;
        UpdateFishButtons();
    }

    private void UpdateFishButtons()
    {
        foreach ((FishType fishType, Button button) in _fishButtons)
        {
            bool isSelected = fishType == _selectedFishType;
            button.BackgroundColor = isSelected ? Color.FromArgb("#0B5D5C") : Color.FromArgb("#EEF4F0");
            button.TextColor = isSelected ? Colors.White : Color.FromArgb("#214039");
            button.BorderColor = isSelected ? Color.FromArgb("#0B5D5C") : Color.FromArgb("#D9E5DF");
            button.BorderWidth = 1;
        }
    }

    private void OnPageSizeChanged(object? sender, EventArgs e)
    {
        UpdateResponsiveLayout();
    }

    private void UpdateResponsiveLayout()
    {
        if (Width <= 0)
        {
            return;
        }

        double contentWidth = Math.Max(320, Math.Min(1240, Width - 32));
        MainContent.WidthRequest = contentWidth;

        bool singleColumnTop = contentWidth < 960;
        ConfigureTopCardsGrid(singleColumnTop);

        int monthColumns = contentWidth < 760 ? 3 : singleColumnTop ? 4 : 6;
        ConfigureMonthlyInputsGrid(monthColumns);

        MainPlot.HeightRequest = singleColumnTop ? 470 : 620;

        if (_dashboard is not null)
        {
            RenderScenarioCards(_dashboard.ScenarioSummaries);
        }
    }

    private void ConfigureTopCardsGrid(bool singleColumn)
    {
        TopCardsGrid.ColumnDefinitions.Clear();
        TopCardsGrid.RowDefinitions.Clear();

        if (singleColumn)
        {
            TopCardsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            TopCardsGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            TopCardsGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            ParametersCard.Row(0).Column(0);
            TemperaturesCard.Row(1).Column(0);
        }
        else
        {
            TopCardsGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1.02, GridUnitType.Star)));
            TopCardsGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1.18, GridUnitType.Star)));
            TopCardsGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            ParametersCard.Row(0).Column(0);
            TemperaturesCard.Row(0).Column(1);
        }
    }

    private void ConfigureMonthlyInputsGrid(int columns)
    {
        columns = Math.Clamp(columns, 1, 6);

        MonthlyInputsGrid.ColumnDefinitions.Clear();
        MonthlyInputsGrid.RowDefinitions.Clear();

        for (int column = 0; column < columns; column++)
        {
            MonthlyInputsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        }

        int rowCount = (int)Math.Ceiling(_monthlyBlocks.Count / (double)columns);

        for (int row = 0; row < rowCount; row++)
        {
            MonthlyInputsGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        }

        for (int index = 0; index < _monthlyBlocks.Count; index++)
        {
            Border monthBlock = _monthlyBlocks[index];
            Grid.SetRow(monthBlock, index / columns);
            Grid.SetColumn(monthBlock, index % columns);
        }
    }

    private async void OnRecalculateClicked(object? sender, EventArgs e)
    {
        await RecalculateAndRenderAsync();
    }

    private async void OnResetDefaultsClicked(object? sender, EventArgs e)
    {
        ApplyInputToUi(SimulationInput.CreateDefault());
        await RecalculateAndRenderAsync();
    }

    private async Task RecalculateAndRenderAsync()
    {
        try
        {
            RecalculateAndRender();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Ошибка данных", ex.Message, "OK");
        }
    }

    private void RecalculateAndRender()
    {
        SimulationInput input = ReadInput();
        _dashboard = SimulationDashboardFactory.Create(input);

        HeroSpeciesLabel.Text = _dashboard.GraphContext.Species.DisplayName;
        HeroSummaryLabel.Text = $"{_dashboard.GraphContext.Results.Count} сценария · {_dashboard.AssignedGraphs.Count} графика";

        RenderScenarioCards(_dashboard.ScenarioSummaries);
        RenderGraphButtons(_dashboard.AssignedGraphs);

        string targetGraphId = _selectedGraph?.Id ?? _dashboard.AssignedGraphs.First().Id;
        SelectGraph(targetGraphId);
    }

    private SimulationInput ReadInput()
    {
        if (!TryParseFlexibleDouble(SpawningTemperatureEntry.Text, out double spawningTemperature))
        {
            throw new InvalidOperationException("Введите корректную температуру нереста.");
        }

        if (!TryParseFlexibleDouble(AnomalyEntry.Text, out double anomaly))
        {
            throw new InvalidOperationException("Введите корректную аномалию года.");
        }

        if (anomaly < 0)
        {
            throw new InvalidOperationException("Аномалия года должна быть положительным числом.");
        }

        List<double> monthlyTemperatures = [];

        for (int i = 0; i < _monthlyEntries.Count; i++)
        {
            if (!TryParseFlexibleDouble(_monthlyEntries[i].Text, out double value))
            {
                throw new InvalidOperationException($"Некорректное значение для месяца {MonthLabels[i]}.");
            }

            monthlyTemperatures.Add(value);
        }

        return new SimulationInput(
            _selectedFishType,
            spawningTemperature,
            anomaly,
            monthlyTemperatures);
    }

    private static bool TryParseFlexibleDouble(string? text, out double value)
    {
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
        {
            return true;
        }

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        string normalized = (text ?? string.Empty).Replace(',', '.');
        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private void RenderScenarioCards(IReadOnlyList<ScenarioSummary> summaries)
    {
        ScenarioCardsGrid.Children.Clear();
        ScenarioCardsGrid.ColumnDefinitions.Clear();
        ScenarioCardsGrid.RowDefinitions.Clear();

        int columns = MainContent.WidthRequest < 760 ? 1 : MainContent.WidthRequest < 1080 ? 2 : 3;

        for (int column = 0; column < columns; column++)
        {
            ScenarioCardsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        }

        int rowCount = (int)Math.Ceiling(summaries.Count / (double)columns);

        for (int row = 0; row < rowCount; row++)
        {
            ScenarioCardsGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        }

        for (int index = 0; index < summaries.Count; index++)
        {
            View card = CreateScenarioCard(summaries[index]);
            Grid.SetRow(card, index / columns);
            Grid.SetColumn(card, index % columns);
            ScenarioCardsGrid.Children.Add(card);
        }
    }

    private View CreateScenarioCard(ScenarioSummary summary)
    {
        Color accent = Color.FromArgb(summary.AccentHex);

        return new Border
        {
            Padding = 16,
            BackgroundColor = Colors.White,
            Stroke = Color.FromArgb("#DCE7E0"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(20) },
            Content = new VerticalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    new Border
                    {
                        Padding = new Thickness(12, 7),
                        BackgroundColor = accent.WithAlpha(0.14f),
                        StrokeThickness = 0,
                        StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(14) },
                        Content = new Label
                        {
                            Text = summary.Name,
                            FontFamily = "OpenSansSemibold",
                            FontSize = 13,
                            TextColor = accent
                        }
                    },
                    CreateMetric("Нерест", $"{summary.SpawningDay} день · {summary.SpawningDate}"),
                    CreateMetric("Личинки", summary.LarvaeDate),
                    CreateMetric("Средняя температура", $"{summary.AverageTemperature:F2} °C"),
                    CreateMetric("Инкубация", $"{summary.IncubationDays:F2} суток")
                }
            }
        };
    }

    private static View CreateMetric(string title, string value)
    {
        return new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            Children =
            {
                new Label
                {
                    Text = title,
                    FontFamily = "OpenSansRegular",
                    FontSize = 12,
                    TextColor = Color.FromArgb("#60756F")
                },
                new Label
                {
                    Text = value,
                    FontFamily = "OpenSansSemibold",
                    FontSize = 12,
                    TextColor = Color.FromArgb("#203E37"),
                    HorizontalTextAlignment = TextAlignment.End
                }.Column(1)
            }
        };
    }

    private void RenderGraphButtons(IReadOnlyList<IGraphBuilder> builders)
    {
        GraphButtonsContainer.Children.Clear();
        _graphButtons.Clear();

        foreach (IGraphBuilder builder in builders)
        {
            var button = new Button
            {
                Text = builder.Title,
                BackgroundColor = Color.FromArgb("#E8F0EB"),
                TextColor = Color.FromArgb("#24423B"),
                CornerRadius = 16,
                Padding = new Thickness(16, 10),
                FontFamily = "OpenSansSemibold",
                FontSize = 13
            };

            button.Clicked += (_, _) => SelectGraph(builder.Id);

            _graphButtons[builder.Id] = button;
            GraphButtonsContainer.Children.Add(button);
        }
    }

    private void SelectGraph(string graphId)
    {
        if (_dashboard is null)
        {
            return;
        }

        IGraphBuilder builder = _dashboard.AssignedGraphs.First(graph => graph.Id.Equals(graphId, StringComparison.OrdinalIgnoreCase));
        _selectedGraph = builder;

        builder.Build(_dashboard.GraphContext, MainPlot.Plot);
        MainPlot.Plot.ScaleFactor = Math.Max(2, DeviceDisplay.MainDisplayInfo.Density);
        MainPlot.Refresh();

        GraphTitleLabel.Text = builder.Title;
        GraphDescriptionLabel.Text = builder.Description;

        foreach ((string id, Button button) in _graphButtons)
        {
            bool isActive = id.Equals(graphId, StringComparison.OrdinalIgnoreCase);
            button.BackgroundColor = isActive ? Color.FromArgb("#0B5D5C") : Color.FromArgb("#E8F0EB");
            button.TextColor = isActive ? Colors.White : Color.FromArgb("#24423B");
        }
    }
}

internal static class GridExtensions
{
    public static T Column<T>(this T view, int column) where T : View
    {
        Grid.SetColumn(view, column);
        return view;
    }

    public static T Row<T>(this T view, int row) where T : View
    {
        Grid.SetRow(view, row);
        return view;
    }
}
