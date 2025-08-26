using System;

namespace DXHistApp.Services
{
    public interface IDialogService
    {
        void ShowError(string message, string title = "Error");
        void ShowWarning(string message, string title = "Warning");
        void ShowInformation(string message, string title = "Information");
        string ShowSaveFileDialog(string title, string defaultExt, string filter, string fileName = null);
        string ShowOpenFileDialog(string title, string defaultExt, string filter);
        bool? ShowDialog<T>(T viewModel) where T : class;
    }
}