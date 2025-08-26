using System.Windows;
using DXHistApp.ViewModels;

namespace DXHistApp.Views
{
    public partial class ChartPropertiesWindow : Window
    {
        public ChartPropertiesWindow()
        {
            InitializeComponent();

            // Handle close request from ViewModel
            Loaded += (s, e) =>
            {
                if (DataContext is ChartPropertiesViewModel viewModel)
                {
                    viewModel.CloseRequested += () => Close();
                }
            };
        }
    }
}