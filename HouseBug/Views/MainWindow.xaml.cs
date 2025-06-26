using System;
using System.Windows;
using System.Windows.Controls;
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
            
            return result == true ? transactionViewModel.GetTransaction() : null;
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

            return saveFileDialog.ShowDialog() == true ? saveFileDialog.FileName : string.Empty;
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

        private void MonthlyBudgetDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Row.Item is MonthlyBudget budget)
            {
                ViewModel.SaveMonthlyBudget(budget);
            }
        }

        private async void AppSettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var settings = ViewModel._budgetManager.GetAppSettings();
            var dialog = new AppSettingsDialog(settings) { Owner = this };
            if (dialog.ShowDialog() == true && dialog.IsSaved)
            {
                await ViewModel._budgetManager.UpdateAppSettingsAsync(settings);
                ViewModel.RefreshCurrencyFromSettings();
                ViewModel.RefreshCommand.Execute(null);
                ViewModel.LoadMonthlySummary();
                ViewModel.RefreshStatisticsPanel();
            }
        }

        private async void TransactionDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Row.Item is Transaction transaction)
            {
                await ViewModel._budgetManager.UpdateTransactionAsync(transaction);
                ViewModel.LoadMonthlySummary();
                ViewModel.StatusMessage = "Transakcja została zaktualizowana";
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            ViewModel.ShowTransactionDialogRequested -= OnShowTransactionDialogRequested;
            ViewModel.ShowConfirmationDialogRequested -= OnShowConfirmationDialogRequested;
            ViewModel.GetSaveFilePathRequested -= OnGetSaveFilePathRequested;
            base.OnClosed(e);
        }
    }
}