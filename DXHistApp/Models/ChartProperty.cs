using System;

namespace DXHistApp.Models
{
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
}