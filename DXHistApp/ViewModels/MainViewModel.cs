using System;
using System.Windows.Input;
using DevExpress.Xpf.Charts;
using DXHistApp.Commands;
using DXHistApp.Services;

namespace DXHistApp.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly IDialogService _dialogService;
        private readonly IChartService _chartService;
        private ChartControl _chartControl;

        public MainViewModel(IDialogService dialogService, IChartService chartService)
        {
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _chartService = chartService ?? throw new ArgumentNullException(nameof(chartService));

            ShowDesignerCommand = new RelayCommand(ShowDesigner, CanExecuteChartCommand);
            ShowPropertiesCommand = new RelayCommand(ShowProperties, CanExecuteChartCommand);
            SaveXmlCommand = new RelayCommand(SaveXml, CanExecuteChartCommand);
            LoadXmlCommand = new RelayCommand(LoadXml, CanExecuteChartCommand);
        }

        public ChartControl ChartControl
        {
            get => _chartControl;
            set
            {
                if (SetProperty(ref _chartControl, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public ICommand ShowDesignerCommand { get; }
        public ICommand ShowPropertiesCommand { get; }
        public ICommand SaveXmlCommand { get; }
        public ICommand LoadXmlCommand { get; }

        private bool CanExecuteChartCommand() => ChartControl != null;

        private void ShowDesigner()
        {
            try
            {
                _chartService.ShowDesigner(ChartControl);
            }
            catch (Exception ex)
            {
                _dialogService.ShowWarning($"Error opening Chart Designer: {ex.Message}", "Chart Designer Error");
            }
        }

        private void ShowProperties()
        {
            try
            {
                var propertiesViewModel = new ChartPropertiesViewModel(ChartControl, _dialogService, _chartService);
                _dialogService.ShowDialog(propertiesViewModel);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"Error showing properties: {ex.Message}", "Properties Error");
            }
        }

        private void SaveXml()
        {
            try
            {
                var fileName = _dialogService.ShowSaveFileDialog(
                    "Save Chart Layout",
                    "xml",
                    "XML Files (*.xml)|*.xml");

                if (!string.IsNullOrEmpty(fileName))
                {
                    _chartService.SaveToFile(ChartControl, fileName);
                    _dialogService.ShowInformation(
                        $"Chart layout saved to:\n{fileName}",
                        "Save Successful");
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"Error saving chart layout: {ex.Message}", "Save Error");
            }
        }

        private void LoadXml()
        {
            try
            {
                var fileName = _dialogService.ShowOpenFileDialog(
                    "Load Chart Layout",
                    "xml",
                    "XML Files (*.xml)|*.xml");

                if (!string.IsNullOrEmpty(fileName))
                {
                    _chartService.LoadFromFile(ChartControl, fileName);
                    _dialogService.ShowInformation(
                        $"Chart layout loaded from:\n{fileName}",
                        "Load Successful");
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"Error loading chart layout: {ex.Message}", "Load Error");
            }
        }
    }
}