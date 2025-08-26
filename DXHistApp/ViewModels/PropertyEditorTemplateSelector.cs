using System.Windows;
using System.Windows.Controls;

namespace DXHistApp.ViewModels
{
    public class PropertyEditorTemplateSelector : DataTemplateSelector
    {
        public DataTemplate BooleanTemplate { get; set; }
        public DataTemplate EnumTemplate { get; set; }
        public DataTemplate TextTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is ChartPropertyViewModel property)
            {
                if (property.IsBoolean)
                    return BooleanTemplate;
                else if (property.IsEnum)
                    return EnumTemplate;
                else
                    return TextTemplate;
            }

            return base.SelectTemplate(item, container);
        }
    }
}