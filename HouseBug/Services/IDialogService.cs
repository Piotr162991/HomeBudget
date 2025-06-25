using System.Windows;
using HouseBug.ViewModels.Base;

namespace HouseBug.Services
{
    public interface IDialogService
    {
        bool ShowConfirmation(string message, string title = "Potwierdzenie");

        string ShowSaveFileDialog(string defaultFileName, string title, string filter);

        void ShowDialog<T>(T viewModel) where T : ViewModelBase;
    }
}
