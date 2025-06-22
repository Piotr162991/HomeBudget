using System.Windows;
using HouseBug.ViewModels;
using HouseBug.Models;

namespace HouseBug.Views.Dialogs
{
    public partial class TransactionDialog : Window
    {
        public Transaction Result { get; private set; }

        public TransactionDialog(TransactionViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            // Subskrypcja wydarzeń z ViewModel
            viewModel.TransactionSaved += OnTransactionSaved;
            viewModel.TransactionCancelled += OnTransactionCancelled;
        }

        private void OnTransactionSaved(object sender, Transaction transaction)
        {
            Result = transaction;
            DialogResult = true;
            Close();
        }

        private void OnTransactionCancelled(object sender, System.EventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}