using System.Windows;
using DXHistApp.Services;
using DXHistApp.ViewModels;

namespace DXHistApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Set up dependency injection
            var dialogService = new DialogService();
            var chartService = new ChartService();
            var viewModel = new MainViewModel(dialogService, chartService);

            DataContext = viewModel;

            // Set the chart control reference after the view is loaded
            Loaded += (s, e) => viewModel.ChartControl = chartControl;
        }
    }
}