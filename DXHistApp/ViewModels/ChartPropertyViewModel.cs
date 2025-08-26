using System;
using System.ComponentModel;
using DXHistApp.Models;

namespace DXHistApp.ViewModels
{
    public class ChartPropertyViewModel : BaseViewModel
    {
        private readonly ChartProperty _model;
        private object _editorValue;

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
        public bool IsText => !IsBoolean && !IsEnum;

        public Array EnumValues => IsEnum ? Enum.GetValues(UnderlyingType) : null;

        public bool HasChanged => !Equals(_model.Value, _editorValue);

        public bool ApplyChanges()
        {
            try
            {
                if (!HasChanged) return false;

                object newValue = ConvertValue(_editorValue);
                _model.PropertyInfo.SetValue(_model.Target, newValue);
                _model.Value = newValue;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private object ConvertValue(object value)
        {
            if (value == null && _model.PropertyType.IsGenericType)
                return null;

            var underlyingType = UnderlyingType;

            if (underlyingType == typeof(int))
                return Convert.ToInt32(value);
            else if (underlyingType == typeof(double))
                return Convert.ToDouble(value);
            else if (underlyingType == typeof(float))
                return Convert.ToSingle(value);
            else if (underlyingType == typeof(bool))
                return Convert.ToBoolean(value);

            return value;
        }
    }
}