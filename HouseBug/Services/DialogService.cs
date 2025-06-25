using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using HouseBug.ViewModels.Base;

namespace HouseBug.Services
{
    public class DialogService : IDialogService
    {
        public bool ShowConfirmation(string message, string title = "Potwierdzenie")
        {
            return MessageBox.Show(
                message, 
                title, 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question) == MessageBoxResult.Yes;
        }

        public string ShowSaveFileDialog(string defaultFileName, string title, string filter)
        {
            var dialog = new SaveFileDialog
            {
                FileName = defaultFileName,
                Title = title,
                Filter = filter
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public void ShowDialog<T>(T viewModel) where T : ViewModelBase
        {
            var window = new Window
            {
                Content = new ContentControl { Content = viewModel },
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow
            };

            window.ShowDialog();
        }
    }
}
