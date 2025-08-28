using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using DevExpress.Xpf.Charts;
using DXHistApp.Commands;
using DXHistApp.Models;
using DXHistApp.Services;

namespace DXHistApp.ViewModels
{
    public class ChartPropertiesViewModel : BaseViewModel
    {
        private readonly ChartControl _chartControl;
        private readonly IDialogService _dialogService;
        private readonly IChartService _chartService;
        private ObservableCollection<ChartPropertyCategoryViewModel> _categories;
        private ObservableCollection<HistogramSelectionItem> _histogramItems;
        private HistogramSelectionItem _selectedHistogram;

        public ChartPropertiesViewModel(ChartControl chartControl, IDialogService dialogService, IChartService chartService)
        {
            _chartControl = chartControl ?? throw new ArgumentNullException(nameof(chartControl));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _chartService = chartService ?? throw new ArgumentNullException(nameof(chartService));

            ApplyChangesCommand = new RelayCommand(ApplyChanges);
            CloseCommand = new RelayCommand(() => CloseRequested?.Invoke());

            LoadProperties();
        }

        public ObservableCollection<ChartPropertyCategoryViewModel> Categories
        {
            get => _categories;
            private set => SetProperty(ref _categories, value);
        }

        public ObservableCollection<HistogramSelectionItem> HistogramItems
        {
            get => _histogramItems;
            private set => SetProperty(ref _histogramItems, value);
        }

        public HistogramSelectionItem SelectedHistogram
        {
            get => _selectedHistogram;
            set
            {
                if (SetProperty(ref _selectedHistogram, value))
                {
                    FilterPropertiesBySelectedHistogram();
                }
            }
        }

        public ICommand ApplyChangesCommand { get; }
        public ICommand CloseCommand { get; }

        public event Action CloseRequested;

        private void LoadProperties()
        {
            // First, build the histogram selection items
            BuildHistogramSelectionItems();

            var properties = new List<ChartProperty>();
            CollectChartProperties(properties);

            var groupedProperties = properties
                .GroupBy(p => p.Category)
                .OrderBy(g => g.Key)
                .Select(g => new ChartPropertyCategoryViewModel(g.Key, g.ToList()))
                .ToList();

            // Expand histogram categories by default
            foreach (var category in groupedProperties)
            {
                if (category.Name.Contains("Histogram") || category.Name == "Scale Options")
                {
                    category.IsExpanded = true;
                }
            }

            Categories = new ObservableCollection<ChartPropertyCategoryViewModel>(groupedProperties);

            // Set default selection to "All Histograms" if available
            if (HistogramItems?.Count > 0)
            {
                SelectedHistogram = HistogramItems.FirstOrDefault(h => h.SeriesIndex == -1) ?? HistogramItems[0];
            }
        }

        private void BuildHistogramSelectionItems()
        {
            var items = new List<HistogramSelectionItem>();

            // Add "All Histograms" option
            items.Add(new HistogramSelectionItem
            {
                DisplayName = "All Histograms",
                SeriesIndex = -1,
                SeriesName = "All"
            });

            // Add individual histogram items
            if (_chartControl.Diagram is XYDiagram2D diagram)
            {
                for (int i = 0; i < diagram.Series.Count; i++)
                {
                    var series = diagram.Series[i];
                    items.Add(new HistogramSelectionItem
                    {
                        DisplayName = $"Series {i + 1} ({series.GetType().Name})",
                        SeriesIndex = i,
                        SeriesName = series.DisplayName ?? $"Series {i + 1}"
                    });
                }
            }

            HistogramItems = new ObservableCollection<HistogramSelectionItem>(items);
        }

        private void FilterPropertiesBySelectedHistogram()
        {
            if (_selectedHistogram == null || Categories == null)
                return;

            foreach (var category in Categories)
            {
                // Show/hide categories based on selection
                if (_selectedHistogram.SeriesIndex == -1) // "All Histograms"
                {
                    // Show all categories
                    category.IsVisible = true;
                }
                else
                {
                    // Show only categories related to the selected histogram
                    string targetSeriesName = $"Series {_selectedHistogram.SeriesIndex + 1}";

                    if (category.Name.Contains("Histogram"))
                    {
                        // Show only the histogram category for the selected series
                        category.IsVisible = category.Name.Contains(targetSeriesName);
                    }
                    else if (category.Name.Contains("Axes") || category.Name.Contains("Scale Options"))
                    {
                        // Show axis properties for the selected series
                        category.IsVisible = category.Name.Contains(targetSeriesName) ||
                                           (!category.Name.Contains("Series") && !category.Name.Contains("X Axis -"));
                    }
                    else if (category.Name == "Series")
                    {
                        // Filter series properties to show only the selected series
                        foreach (var property in category.Properties)
                        {
                            property.IsVisible = property.FullName.Contains(targetSeriesName);
                        }
                        category.IsVisible = category.Properties.Any(p => p.IsVisible);
                    }
                    else
                    {
                        // Show general categories (Chart, Diagram, etc.)
                        category.IsVisible = !category.Name.Contains("Series") && !category.Name.Contains("Histogram");
                    }
                }
            }
        }

        private void CollectChartProperties(List<ChartProperty> properties)
        {
            // Chart Control properties
            AddObjectProperties(properties, _chartControl, "Chart", "Chart Control");

            // Ensure Diagram exists
            if (_chartControl.Diagram == null)
                _chartControl.Diagram = new XYDiagram2D();

            if (_chartControl.Diagram is XYDiagram2D diagram)
            {
                AddObjectProperties(properties, diagram, "Diagram", "XY Diagram");

                // Handle each series separately for histogram properties
                for (int seriesIndex = 0; seriesIndex < diagram.Series.Count; seriesIndex++)
                {
                    var series = diagram.Series[seriesIndex];
                    ProcessSeriesHistogramProperties(properties, diagram, series, seriesIndex);
                }

                // Global Y-axis (usually shared)
                if (diagram.AxisY == null)
                    diagram.AxisY = new AxisY2D();

                AddObjectProperties(properties, diagram.AxisY, "Axes", "Y Axis");

                if (diagram.AxisY.NumericScaleOptions == null)
                    diagram.AxisY.NumericScaleOptions = new ContinuousNumericScaleOptions();

                AddObjectProperties(properties, diagram.AxisY.NumericScaleOptions, "Scale Options", "Y Axis Scale");
            }
        }

        private void ProcessSeriesHistogramProperties(List<ChartProperty> properties, XYDiagram2D diagram, Series series, int seriesIndex)
        {
            string seriesName = $"Series {seriesIndex + 1}";
            string histogramCategory = $"Histogram - {seriesName}";

            // Add general series properties
            AddObjectProperties(properties, series, "Series", $"{seriesName} ({series.GetType().Name})");

            // Create or get X-axis for this series
            AxisX2D seriesAxisX = GetOrCreateAxisXForSeries(diagram, series, seriesIndex);

            // Ensure NumericScaleOptions exists for this axis
            if (seriesAxisX.NumericScaleOptions == null)
            {
                seriesAxisX.NumericScaleOptions = new CountIntervalNumericScaleOptions()
                {
                    AggregateFunction = AggregateFunction.Histogram,
                    Count = diagram.Series.Count > 0 && diagram.Series[0] is XYSeries firstSeries && firstSeries.Points.Count > 0
                        ? firstSeries.Points.Count
                        : 10
                };
            }

            // Add X-axis properties for this series
            AddObjectProperties(properties, seriesAxisX, "Axes", $"X Axis - {seriesName}");
            AddObjectProperties(properties, seriesAxisX.NumericScaleOptions, "Scale Options", $"X Axis Scale - {seriesName}");

            // Add histogram-specific properties if it's a CountIntervalNumericScaleOptions
            if (seriesAxisX.NumericScaleOptions is CountIntervalNumericScaleOptions countOptions)
            {
                // Bin Count
                AddSpecialProperty(properties, $"Bin Count", histogramCategory,
                    countOptions.Count ?? 15, typeof(int),
                    countOptions, typeof(CountIntervalNumericScaleOptions).GetProperty("Count"),
                    $"Number of bins for {seriesName} histogram");

                // Underflow Value
                AddSpecialProperty(properties, $"Underflow Value", histogramCategory,
                    countOptions.UnderflowValue, typeof(double?),
                    countOptions, typeof(CountIntervalNumericScaleOptions).GetProperty("UnderflowValue"),
                    $"Values below this go into the underflow bin for {seriesName}");

                // Overflow Value
                AddSpecialProperty(properties, $"Overflow Value", histogramCategory,
                    countOptions.OverflowValue, typeof(double?),
                    countOptions, typeof(CountIntervalNumericScaleOptions).GetProperty("OverflowValue"),
                    $"Values above this go into the overflow bin for {seriesName}");

                // Aggregate Function (usually Histogram, but allow changing)
                AddSpecialProperty(properties, $"Aggregate Function", histogramCategory,
                    countOptions.AggregateFunction, typeof(AggregateFunction),
                    countOptions, typeof(CountIntervalNumericScaleOptions).GetProperty("AggregateFunction"),
                    $"Aggregation function for {seriesName}");
            }
        }

        private AxisX2D GetOrCreateAxisXForSeries(XYDiagram2D diagram, Series series, int seriesIndex)
        {
            // For the first series, use the primary X-axis
            if (seriesIndex == 0)
            {
                if (diagram.AxisX == null)
                    diagram.AxisX = new AxisX2D();
                return diagram.AxisX;
            }

            // For additional series, check if we need separate axes
            // If series have different data ranges or require different bin configurations,
            // we should use secondary axes

            // First, try to find an existing secondary axis for this series
            SecondaryAxisX2D existingSecondaryAxis = null;
            for (int i = 0; i < diagram.SecondaryAxesX.Count; i++)
            {
                var axis = diagram.SecondaryAxesX[i];
                if (axis.Name == $"AxisX_Series_{seriesIndex}")
                {
                    existingSecondaryAxis = axis;
                    break;
                }
            }

            if (existingSecondaryAxis != null)
            {
                return existingSecondaryAxis;
            }

            // Create a new secondary X-axis for this series
            var secondaryAxisX = new SecondaryAxisX2D()
            {
                Name = $"AxisX_Series_{seriesIndex}",
                Alignment = AxisAlignment.Near, // You can adjust this based on your needs
                Visible = false // Usually hidden for histograms, but can be made visible if needed
            };

            diagram.SecondaryAxesX.Add(secondaryAxisX);

            // Assign this axis to the series
            if (series is XYSeries xySeries)
            {
                XYDiagram2D.SetSeriesAxisX(xySeries, secondaryAxisX);
            }

            return secondaryAxisX;
        }

        private void AddObjectProperties(List<ChartProperty> properties, object obj, string category, string prefix)
        {
            if (obj == null) return;

            var props = obj.GetType().GetProperties()
                .Where(p => p.CanRead && p.CanWrite && IsEditableProperty(p))
                .ToList();

            foreach (var prop in props)
            {
                try
                {
                    var value = prop.GetValue(obj);
                    properties.Add(new ChartProperty
                    {
                        Name = $"{prefix} - {prop.Name}",
                        Category = category,
                        Value = value,
                        PropertyType = prop.PropertyType,
                        Target = obj,
                        PropertyInfo = prop,
                        Description = GetPropertyDescription(prop)
                    });
                }
                catch { /* Skip properties that can't be read */ }
            }
        }

        private void AddSpecialProperty(List<ChartProperty> properties, string name, string category, object value, Type type,
            object target, System.Reflection.PropertyInfo propInfo, string description)
        {
            properties.Add(new ChartProperty
            {
                Name = name,
                Category = category,
                Value = value,
                PropertyType = type,
                Target = target,
                PropertyInfo = propInfo,
                Description = description
            });
        }

        private bool IsEditableProperty(System.Reflection.PropertyInfo prop)
        {
            var type = prop.PropertyType;
            return type == typeof(string) ||
                   type == typeof(int) || type == typeof(int?) ||
                   type == typeof(double) || type == typeof(double?) ||
                   type == typeof(float) || type == typeof(float?) ||
                   type == typeof(bool) || type == typeof(bool?) ||
                   type.IsEnum ||
                   (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                    type.GetGenericArguments()[0].IsEnum);
        }

        private string GetPropertyDescription(System.Reflection.PropertyInfo prop)
        {
            var description = prop.GetCustomAttributes(typeof(DescriptionAttribute), false)
                .Cast<DescriptionAttribute>()
                .FirstOrDefault()?.Description;

            return string.IsNullOrEmpty(description) ? $"{prop.Name} property" : description;
        }

        private void ApplyChanges()
        {
            try
            {
                int changedCount = 0;

                foreach (var category in Categories)
                {
                    foreach (var property in category.Properties)
                    {
                        if (property.ApplyChanges())
                        {
                            changedCount++;
                        }
                    }
                }

                // Force chart refresh after applying histogram changes
                _chartService.UpdateChart(_chartControl);

                // Refresh the histogram calculations if needed
                if (_chartControl.Diagram is XYDiagram2D diagram)
                {
                    // Force refresh of the chart data to recalculate histogram bins
                    _chartControl.UpdateData();
                }

                _dialogService.ShowInformation(
                    $"Applied {changedCount} property changes successfully.",
                    "Changes Applied");
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"Error applying changes: {ex.Message}", "Apply Error");
            }
        }
    }

    public class ChartPropertyCategoryViewModel : BaseViewModel
    {
        private bool _isExpanded;
        private bool _isVisible = true;

        public ChartPropertyCategoryViewModel(string name, List<ChartProperty> properties)
        {
            Name = name;
            Properties = new ObservableCollection<ChartPropertyViewModel>(
                properties.Select(p => new ChartPropertyViewModel(p)).OrderBy(p => p.Name));
            Header = $"{name} ({properties.Count} properties)";
        }

        public string Name { get; }
        public string Header { get; }
        public ObservableCollection<ChartPropertyViewModel> Properties { get; }

        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }
    }

    public class HistogramSelectionItem
    {
        public string DisplayName { get; set; }
        public int SeriesIndex { get; set; } // -1 for "All"
        public string SeriesName { get; set; }

        public override string ToString() => DisplayName;
    }
}