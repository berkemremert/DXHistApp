using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using DevExpress.Xpf.Charts;
using DevExpress.Charts.Designer;
using DevExpress.Xpf.Dialogs;
using MessageBox = System.Windows.MessageBox;
using TextBox = System.Windows.Controls.TextBox;
using Orientation = System.Windows.Controls.Orientation;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using ComboBox = System.Windows.Controls.ComboBox;

namespace DXHistApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnShowDesigner_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var designer = new ChartDesigner(chartControl);
                designer.Show(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening Chart Designer: {ex.Message}",
                    "Chart Designer Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnShowProperties_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var propertiesWindow = new ChartPropertiesWindow(chartControl);
                propertiesWindow.Owner = this;
                propertiesWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error showing properties: {ex.Message}",
                    "Properties Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSaveXml_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new DXSaveFileDialog
                {
                    Title = "Save Chart Layout",
                    DefaultExt = "xml",
                    Filter = "XML Files (*.xml)|*.xml",
                    FileName = $"ChartLayout_{DateTime.Now:yyyyMMdd_HHmmss}.xml"
                };

                if (dialog.ShowDialog() == true)
                {
                    chartControl.SaveToFile(dialog.FileName);
                    MessageBox.Show($"Chart layout saved to:\n{dialog.FileName}",
                        "Save Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving chart layout: {ex.Message}",
                    "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnLoadXml_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new DXOpenFileDialog
                {
                    Title = "Load Chart Layout",
                    DefaultExt = "xml",
                    Filter = "XML Files (*.xml)|*.xml"
                };

                if (dialog.ShowDialog() == true)
                {
                    chartControl.LoadFromFile(dialog.FileName);
                    MessageBox.Show($"Chart layout loaded from:\n{dialog.FileName}",
                        "Load Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading chart layout: {ex.Message}",
                    "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class DataPoint : INotifyPropertyChanged
    {
        private double _value;

        public double Value
        {
            get => _value;
            set
            {
                _value = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class ChartProperty
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public object Value { get; set; }
        public Type PropertyType { get; set; }
        public object Target { get; set; }
        public System.Reflection.PropertyInfo PropertyInfo { get; set; }
        public string Description { get; set; }
    }

    public partial class ChartPropertiesWindow : Window
    {
        private ChartControl _chartControl;
        private List<ChartProperty> _properties;
        private Dictionary<string, StackPanel> _categoryPanels;

        public ChartPropertiesWindow(ChartControl chartControl)
        {
            _chartControl = chartControl;
            InitializeComponent();
            LoadProperties();
        }

        private void InitializeComponent()
        {
            Title = "Chart Properties Editor";
            Width = 600;
            Height = 700;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Scroll viewer for properties
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(10)
            };

            var propertiesPanel = new StackPanel();
            scrollViewer.Content = propertiesPanel;
            Grid.SetRow(scrollViewer, 0);
            mainGrid.Children.Add(scrollViewer);

            // Buttons panel
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(10)
            };

            var applyButton = new Button
            {
                Content = "Apply Changes",
                Width = 100,
                Height = 30,
                Margin = new Thickness(5)
            };
            applyButton.Click += ApplyButton_Click;

            var closeButton = new Button
            {
                Content = "Close",
                Width = 75,
                Height = 30,
                Margin = new Thickness(5)
            };
            closeButton.Click += (s, e) => Close();

            buttonPanel.Children.Add(applyButton);
            buttonPanel.Children.Add(closeButton);
            Grid.SetRow(buttonPanel, 1);
            mainGrid.Children.Add(buttonPanel);

            Content = mainGrid;

            _categoryPanels = new Dictionary<string, StackPanel>();
        }

        private void LoadProperties()
        {
            _properties = new List<ChartProperty>();

            // Get main properties panel
            var mainPanel = ((ScrollViewer)((Grid)Content).Children[0]).Content as StackPanel;
            mainPanel.Children.Clear();
            _categoryPanels.Clear();

            // Collect all chart properties
            CollectChartProperties();

            // Group properties by category and create UI
            var groupedProperties = _properties.GroupBy(p => p.Category).OrderBy(g => g.Key);

            foreach (var group in groupedProperties)
            {
                CreateCategoryPanel(mainPanel, group.Key, group.ToList());
            }
        }

        private void CollectChartProperties()
        {
            // Chart Control properties
            AddObjectProperties(_chartControl, "Chart", "Chart Control");

            // Ensure Diagram exists
            if (_chartControl.Diagram == null)
                _chartControl.Diagram = new XYDiagram2D();

            if (_chartControl.Diagram is XYDiagram2D diagram)
            {
                AddObjectProperties(diagram, "Diagram", "XY Diagram");

                // --- Axis X ---
                if (diagram.AxisX == null)
                    diagram.AxisX = new AxisX2D();

                AddObjectProperties(diagram.AxisX, "Axes", "X Axis");

                if (diagram.AxisX.NumericScaleOptions == null)
                    diagram.AxisX.NumericScaleOptions = new CountIntervalNumericScaleOptions()
                    {
                        AggregateFunction = AggregateFunction.Histogram,
                        Count = 10 // default bin count
                    };

                AddObjectProperties(diagram.AxisX.NumericScaleOptions, "Scale Options", "X Axis Scale");

                if (diagram.AxisX.NumericScaleOptions is CountIntervalNumericScaleOptions countOptions)
                {
                    AddSpecialProperty("Bin Count", "Histogram", countOptions.Count ?? 15, typeof(int),
                        countOptions, typeof(CountIntervalNumericScaleOptions).GetProperty("Count"),
                        "Number of bins for histogram");

                    AddSpecialProperty("Underflow Value", "Histogram", countOptions.UnderflowValue, typeof(double?),
                        countOptions, typeof(CountIntervalNumericScaleOptions).GetProperty("UnderflowValue"),
                        "Values below this go into the underflow bin");

                    AddSpecialProperty("Overflow Value", "Histogram", countOptions.OverflowValue, typeof(double?),
                        countOptions, typeof(CountIntervalNumericScaleOptions).GetProperty("OverflowValue"),
                        "Values above this go into the overflow bin");
                }

                // --- Axis Y ---
                if (diagram.AxisY == null)
                    diagram.AxisY = new AxisY2D();

                AddObjectProperties(diagram.AxisY, "Axes", "Y Axis");

                if (diagram.AxisY.NumericScaleOptions == null)
                    diagram.AxisY.NumericScaleOptions = new ContinuousNumericScaleOptions();

                AddObjectProperties(diagram.AxisY.NumericScaleOptions, "Scale Options", "Y Axis Scale");

                // --- Series ---
                for (int i = 0; i < diagram.Series.Count; i++)
                {
                    var series = diagram.Series[i];
                    AddObjectProperties(series, "Series", $"Series {i + 1} ({series.GetType().Name})");
                }
            }
        }

        private void AddObjectProperties(object obj, string category, string prefix)
        {
            if (obj == null) return;

            var properties = obj.GetType().GetProperties()
                .Where(p => p.CanRead && p.CanWrite && IsEditableProperty(p))
                .ToList();

            foreach (var prop in properties)
            {
                try
                {
                    var value = prop.GetValue(obj);
                    _properties.Add(new ChartProperty
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

        private void AddSpecialProperty(string name, string category, object value, Type type,
            object target, System.Reflection.PropertyInfo propInfo, string description)
        {
            _properties.Add(new ChartProperty
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

        private void CreateCategoryPanel(StackPanel parent, string categoryName, List<ChartProperty> properties)
        {
            var expander = new Expander
            {
                Header = $"{categoryName} ({properties.Count} properties)",
                IsExpanded = categoryName == "Histogram" || categoryName == "Scale Options",
                Margin = new Thickness(0, 5, 0, 5)
            };

            var categoryPanel = new StackPanel();
            expander.Content = categoryPanel;

            foreach (var property in properties.OrderBy(p => p.Name))
            {
                CreatePropertyEditor(categoryPanel, property);
            }

            parent.Children.Add(expander);
            _categoryPanels[categoryName] = categoryPanel;
        }

        private void CreatePropertyEditor(StackPanel parent, ChartProperty property)
        {
            var grid = new Grid
            {
                Margin = new Thickness(5)
            };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Label
            var label = new TextBlock
            {
                Text = property.Name.Split('-').Last().Trim(),
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = property.Description
            };
            Grid.SetColumn(label, 0);
            grid.Children.Add(label);

            // Editor
            var editor = CreateEditorControl(property);
            Grid.SetColumn(editor, 1);
            grid.Children.Add(editor);

            parent.Children.Add(grid);
        }

        private FrameworkElement CreateEditorControl(ChartProperty property)
        {
            var type = property.PropertyType;
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            if (underlyingType == typeof(bool))
            {
                var checkBox = new CheckBox
                {
                    IsChecked = (bool?)(property.Value),
                    Tag = property
                };
                return checkBox;
            }
            else if (underlyingType.IsEnum)
            {
                var comboBox = new ComboBox
                {
                    ItemsSource = Enum.GetValues(underlyingType),
                    SelectedItem = property.Value,
                    Tag = property
                };
                return comboBox;
            }
            else
            {
                var textBox = new TextBox
                {
                    Text = property.Value?.ToString() ?? "",
                    Tag = property
                };
                return textBox;
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int changedCount = 0;

                foreach (var panel in _categoryPanels.Values)
                {
                    changedCount += ApplyPanelChanges(panel);
                }

                _chartControl.UpdateData();
                MessageBox.Show($"Applied {changedCount} property changes successfully.",
                    "Changes Applied", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying changes: {ex.Message}",
                    "Apply Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int ApplyPanelChanges(StackPanel panel)
        {
            int changeCount = 0;

            foreach (Grid grid in panel.Children.OfType<Grid>())
            {
                var editor = grid.Children.OfType<FrameworkElement>().FirstOrDefault(c => c.Tag is ChartProperty);
                if (editor?.Tag is ChartProperty property)
                {
                    if (ApplyPropertyChange(editor, property))
                    {
                        changeCount++;
                    }
                }
            }

            return changeCount;
        }

        private bool ApplyPropertyChange(FrameworkElement editor, ChartProperty property)
        {
            try
            {
                object newValue = null;
                var type = property.PropertyType;
                var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

                if (editor is CheckBox checkBox)
                {
                    newValue = checkBox.IsChecked;
                }
                else if (editor is ComboBox comboBox)
                {
                    newValue = comboBox.SelectedItem;
                }
                else if (editor is TextBox textBox)
                {
                    var text = textBox.Text;
                    if (string.IsNullOrWhiteSpace(text) && type.IsGenericType)
                    {
                        newValue = null;
                    }
                    else if (underlyingType == typeof(int))
                    {
                        newValue = int.Parse(text);
                    }
                    else if (underlyingType == typeof(double))
                    {
                        newValue = double.Parse(text);
                    }
                    else if (underlyingType == typeof(float))
                    {
                        newValue = float.Parse(text);
                    }
                    else
                    {
                        newValue = text;
                    }
                }

                if (!Equals(property.Value, newValue))
                {
                    property.PropertyInfo.SetValue(property.Target, newValue);
                    property.Value = newValue;
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting {property.Name}: {ex.Message}",
                    "Property Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            return false;
        }
    }
}