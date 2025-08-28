using System;
using System.ComponentModel;
using System.Linq;
using DXHistApp.Models;

namespace DXHistApp.ViewModels
{
    public class ChartPropertyViewModel : BaseViewModel
    {
        private readonly ChartProperty _model;
        private object _editorValue;
        private bool _isVisible = true;

        public ChartPropertyViewModel(ChartProperty model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _editorValue = model.Value;
        }

        public string Name => _model.Name.Split('-').Last().Trim();
        public string FullName => _model.Name;
        public string Category => _model.Category;
        public string Description => _model.Description;
        public Type PropertyType => _model.PropertyType;
        public Type UnderlyingType => Nullable.GetUnderlyingType(_model.PropertyType) ?? _model.PropertyType;

        public object EditorValue
        {
            get => _editorValue;
            set => SetProperty(ref _editorValue, value);
        }

        public bool IsBoolean => UnderlyingType == typeof(bool);
        public bool IsEnum => UnderlyingType.IsEnum;
        public bool IsNumeric => IsInt || IsDouble || IsFloat;
        public bool IsInt => UnderlyingType == typeof(int);
        public bool IsDouble => UnderlyingType == typeof(double);
        public bool IsFloat => UnderlyingType == typeof(float);
        public bool IsText => !IsBoolean && !IsEnum && !IsNumeric;
        public bool IsNullable => _model.PropertyType.IsGenericType &&
                                 _model.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>);

        public Array EnumValues => IsEnum ? Enum.GetValues(UnderlyingType) : null;

        public bool HasChanged => !Equals(_model.Value, _editorValue);

        // For histogram-specific properties, provide better validation
        public bool IsHistogramProperty => _model.Category.Contains("Histogram");
        public bool IsBinCount => Name.Contains("Bin Count");
        public bool IsUnderflowValue => Name.Contains("Underflow");
        public bool IsOverflowValue => Name.Contains("Overflow");

        // Validation for histogram properties
        public string ValidationError
        {
            get
            {
                if (!IsHistogramProperty || _editorValue == null)
                    return null;

                if (IsBinCount && IsInt)
                {
                    if (Convert.ToInt32(_editorValue) <= 0)
                        return "Bin count must be greater than 0";
                    if (Convert.ToInt32(_editorValue) > 1000)
                        return "Bin count should not exceed 1000 for performance reasons";
                }

                if ((IsUnderflowValue || IsOverflowValue) && IsDouble)
                {
                    // Basic validation for overflow/underflow values
                    var doubleValue = Convert.ToDouble(_editorValue);
                    if (double.IsNaN(doubleValue))
                        return "Value cannot be NaN";
                    if (double.IsInfinity(doubleValue))
                        return "Value cannot be infinity";
                }

                return null;
            }
        }

        public bool IsValid => string.IsNullOrEmpty(ValidationError);

        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        public bool ApplyChanges()
        {
            try
            {
                if (!HasChanged) return false;
                if (!IsValid) return false;

                object newValue = ConvertValue(_editorValue);
                _model.PropertyInfo.SetValue(_model.Target, newValue);
                _model.Value = newValue;

                // Notify property changed to update UI
                OnPropertyChanged(nameof(HasChanged));

                return true;
            }
            catch (Exception ex)
            {
                // You might want to log this exception
                System.Diagnostics.Debug.WriteLine($"Error applying property change for {Name}: {ex.Message}");
                return false;
            }
        }

        private object ConvertValue(object value)
        {
            if (value == null && IsNullable)
                return null;

            if (value == null && !IsNullable)
                throw new InvalidOperationException($"Cannot set null value for non-nullable property {Name}");

            var underlyingType = UnderlyingType;

            try
            {
                if (underlyingType == typeof(int))
                {
                    return Convert.ToInt32(value);
                }
                else if (underlyingType == typeof(double))
                {
                    return Convert.ToDouble(value);
                }
                else if (underlyingType == typeof(float))
                {
                    return Convert.ToSingle(value);
                }
                else if (underlyingType == typeof(bool))
                {
                    return Convert.ToBoolean(value);
                }
                else if (underlyingType.IsEnum)
                {
                    if (value is string stringValue)
                        return Enum.Parse(underlyingType, stringValue);
                    return Enum.ToObject(underlyingType, value);
                }

                return value;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Cannot convert value '{value}' to type {underlyingType.Name} for property {Name}", ex);
            }
        }

        // Helper method to reset to original value
        public void ResetValue()
        {
            EditorValue = _model.Value;
            OnPropertyChanged(nameof(HasChanged));
        }

        // Helper method to get display text for current value
        public string GetDisplayValue()
        {
            if (_editorValue == null)
                return IsNullable ? "(null)" : "N/A";

            if (IsEnum)
                return _editorValue.ToString();

            if (IsDouble || IsFloat)
                return string.Format("{0:F2}", _editorValue);

            return _editorValue.ToString();
        }
    }
}