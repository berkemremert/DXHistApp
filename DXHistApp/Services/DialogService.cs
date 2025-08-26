using System;
using System.Windows;
using DevExpress.Xpf.Dialogs;
using DXHistApp.Views;
using DXHistApp.ViewModels;
using MessageBox = System.Windows.MessageBox;
using Application = System.Windows.Application;

namespace DXHistApp.Services
{
    public class DialogService : IDialogService
    {
        public void ShowError(string message, string title = "Error")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void ShowWarning(string message, string title = "Warning")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public void ShowInformation(string message, string title = "Information")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public string ShowSaveFileDialog(string title, string defaultExt, string filter, string fileName = null)
        {
            var dialog = new DXSaveFileDialog
            {
                Title = title,
                DefaultExt = defaultExt,
                Filter = filter,
                FileName = fileName ?? $"ChartLayout_{DateTime.Now:yyyyMMdd_HHmmss}.{defaultExt}"
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public string ShowOpenFileDialog(string title, string defaultExt, string filter)
        {
            var dialog = new DXOpenFileDialog
            {
                Title = title,
                DefaultExt = defaultExt,
                Filter = filter
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public bool? ShowDialog<T>(T viewModel) where T : class
        {
            Window dialog = null;

            if (viewModel is ChartPropertiesViewModel propertiesViewModel)
            {
                dialog = new ChartPropertiesWindow
                {
                    DataContext = propertiesViewModel,
                    Owner = Application.Current.MainWindow
                };
            }

            return dialog?.ShowDialog();
        }
    }
}