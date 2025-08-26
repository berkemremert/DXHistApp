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

        public ICommand ApplyChangesCommand { get; }
        public ICommand CloseCommand { get; }

        public event Action CloseRequested;

        private void LoadProperties()
        {
            var properties = new List<ChartProperty>();
            CollectChartProperties(properties);

            var groupedProperties = properties
                .GroupBy(p => p.Category)
                .OrderBy(g => g.Key)
                .Select(g => new ChartPropertyCategoryViewModel(g.Key, g.ToList()))
                .ToList();

            // Expand certain categories by default
            foreach (var category in groupedProperties)
            {
                if (category.Name == "Histogram" || category.Name == "Scale Options")
                {
                    category.IsExpanded = true;
                }
            }

            Categories = new ObservableCollection<ChartPropertyCategoryViewModel>(groupedProperties);
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

                // --- Axis X ---
                if (diagram.AxisX == null)
                    diagram.AxisX = new AxisX2D();

                AddObjectProperties(properties, diagram.AxisX, "Axes", "X Axis");

                if (diagram.AxisX.NumericScaleOptions == null)
                    diagram.AxisX.NumericScaleOptions = new CountIntervalNumericScaleOptions()
                    {
                        AggregateFunction = AggregateFunction.Histogram,
                        Count = diagram.Series.Count > 0 && diagram.Series[0].Points.Count > 0
                        ? diagram.Series[0].Points.Count
                        : 10
                    };

                AddObjectProperties(properties, diagram.AxisX.NumericScaleOptions, "Scale Options", "X Axis Scale");

                if (diagram.AxisX.NumericScaleOptions is CountIntervalNumericScaleOptions countOptions)
                {
                    AddSpecialProperty(properties, "Bin Count", "Histogram", countOptions.Count ?? 15, typeof(int),
                        countOptions, typeof(CountIntervalNumericScaleOptions).GetProperty("Count"),
                        "Number of bins for histogram");

                    AddSpecialProperty(properties, "Underflow Value", "Histogram", countOptions.UnderflowValue, typeof(double?),
                        countOptions, typeof(CountIntervalNumericScaleOptions).GetProperty("UnderflowValue"),
                        "Values below this go into the underflow bin");

                    AddSpecialProperty(properties, "Overflow Value", "Histogram", countOptions.OverflowValue, typeof(double?),
                        countOptions, typeof(CountIntervalNumericScaleOptions).GetProperty("OverflowValue"),
                        "Values above this go into the overflow bin");
                }

                // --- Axis Y ---
                if (diagram.AxisY == null)
                    diagram.AxisY = new AxisY2D();

                AddObjectProperties(properties, diagram.AxisY, "Axes", "Y Axis");

                if (diagram.AxisY.NumericScaleOptions == null)
                    diagram.AxisY.NumericScaleOptions = new ContinuousNumericScaleOptions();

                AddObjectProperties(properties, diagram.AxisY.NumericScaleOptions, "Scale Options", "Y Axis Scale");

                // --- Series ---
                for (int i = 0; i < diagram.Series.Count; i++)
                {
                    var series = diagram.Series[i];
                    AddObjectProperties(properties, series, "Series", $"Series {i + 1} ({series.GetType().Name})");
                }
            }
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

                _chartService.UpdateChart(_chartControl);
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
    }
}