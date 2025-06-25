using System;
using System.Windows;
using HouseBug.ViewModels;
using HouseBug.Views.Dialogs;
using HouseBug.Models;
using Microsoft.Win32;

namespace HouseBug.Views
{
    public partial class MainWindow : Window
    {
        private MainViewModel ViewModel => (MainViewModel)DataContext;

        public MainWindow()
        {
            InitializeComponent();
            
            ViewModel.ShowTransactionDialogRequested += OnShowTransactionDialogRequested;
            ViewModel.ShowConfirmationDialogRequested += OnShowConfirmationDialogRequested;
            ViewModel.GetSaveFilePathRequested += OnGetSaveFilePathRequested;
        }

        private Transaction OnShowTransactionDialogRequested(TransactionViewModel transactionViewModel)
        {
            var dialog = new TransactionDialog(transactionViewModel)
            {
                Owner = this
            };

            var result = dialog.ShowDialog();
            
            if (result == true)
            {
                return transactionViewModel.GetTransaction();
            }

            return null;
        }

        private bool OnShowConfirmationDialogRequested(string message)
        {
            var result = MessageBox.Show(message, "Potwierdzenie", MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }

        private string OnGetSaveFilePathRequested(string extension)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = extension.ToUpper() + " files (*." + extension + ")|*." + extension,
                DefaultExt = extension
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                return saveFileDialog.FileName;
            }

            return null;
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CategoriesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var categoryWindow = new CategoryWindow
            {
                Owner = this
            };
            categoryWindow.ShowDialog();
            
            ViewModel.RefreshCommand.Execute(null);
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("HouseBug - Budżet Domowy\nWersja 1.0\n\nProsta aplikacja do zarządzania budżetem domowym.", 
                          "O programie", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.ShowTransactionDialogRequested -= OnShowTransactionDialogRequested;
                ViewModel.ShowConfirmationDialogRequested -= OnShowConfirmationDialogRequested;
                ViewModel.GetSaveFilePathRequested -= OnGetSaveFilePathRequested;
            }
            
            base.OnClosed(e);
        }
    }
}