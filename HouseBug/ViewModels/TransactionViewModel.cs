using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Input;
using HouseBug.Models;
using HouseBug.Services;
using HouseBug.ViewModels.Base;

namespace HouseBug.ViewModels
{
    public class TransactionViewModel : ViewModelBase, IDataErrorInfo
    {
        private readonly BudgetManager _budgetManager;
        private readonly Transaction _originalTransaction;
        private bool _isEditMode;

        public TransactionViewModel(BudgetManager budgetManager, List<Category> categories, Transaction transaction = null)
        {
            _budgetManager = budgetManager;
            Categories = categories ?? new List<Category>();
            _originalTransaction = transaction;
            _isEditMode = transaction != null;

            InitializeCommands();
            InitializeFromTransaction(transaction);
        }


        #region Properties

        public List<Category> Categories { get; }

        private decimal _amount;
        [Required(ErrorMessage = "Kwota jest wymagana")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Kwota musi być większa od 0")]
        public decimal Amount
        {
            get => _amount;
            set => SetProperty(ref _amount, value);
        }

        private string _description;
        [Required(ErrorMessage = "Opis jest wymagany")]
        [StringLength(200, ErrorMessage = "Opis nie może być dłuższy niż 200 znaków")]
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private DateTime _date = DateTime.Today;
        public DateTime Date
        {
            get => _date;
            set => SetProperty(ref _date, value);
        }

        private Category _selectedCategory;
        [Required(ErrorMessage = "Kategoria jest wymagana")]
        public Category SelectedCategory
        {
            get => _selectedCategory;
            set => SetProperty(ref _selectedCategory, value);
        }

        private bool _isIncome;
        public bool IsIncome
        {
            get => _isIncome;
            set => SetProperty(ref _isIncome, value);
        }

        public bool IsExpense
        {
            get => !_isIncome;
            set => IsIncome = !value;
        }

        private string _validationMessage;
        public string ValidationMessage
        {
            get => _validationMessage;
            set => SetProperty(ref _validationMessage, value);
        }

        public bool IsEditMode => _isEditMode;

        public string WindowTitle => IsEditMode ? "Edytuj transakcję" : "Dodaj transakcję";

        #endregion

        #region Commands

        public ICommand SaveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public ICommand SetIncomeCommand { get; private set; }
        public ICommand SetExpenseCommand { get; private set; }

        private void InitializeCommands()
        {
            SaveCommand = new RelayCommand(Save, CanSave);
            CancelCommand = new RelayCommand(Cancel);
            SetIncomeCommand = new RelayCommand(() => IsIncome = true);
            SetExpenseCommand = new RelayCommand(() => IsIncome = false);
        }

        #endregion

        #region Command Methods

        private async void Save()
        {
            if (!IsValid()) return;

            SetBusy(true, IsEditMode ? "Aktualizowanie transakcji..." : "Zapisywanie transakcji...");

            try
            {
                var transaction = CreateTransactionFromInput();

                if (IsEditMode)
                {
                    transaction.Id = _originalTransaction.Id;
                    transaction.CreatedAt = _originalTransaction.CreatedAt;
                    transaction.UpdatedAt = DateTime.Now;

                    var success = await _budgetManager.UpdateTransactionAsync(transaction);
                    if (success)
                    {
                        OnTransactionSaved(transaction);
                    }
                    else
                    {
                        ValidationMessage = "Błąd podczas aktualizacji transakcji.";
                    }
                }
                else
                {
                    var savedTransaction = await _budgetManager.AddTransactionAsync(transaction);
                    OnTransactionSaved(savedTransaction);
                }
            }
            catch (Exception ex)
            {
                ValidationMessage = $"Wystąpił błąd: {ex.Message}";
            }
            finally
            {
                SetBusy(false);
            }
        }

        private bool CanSave()
        {
            return !IsBusy && IsValid();
        }

        private void Cancel()
        {
            OnTransactionCancelled();
        }

        #endregion

        #region Validation

        public string Error => string.Empty;

        public string this[string columnName]
        {
            get
            {
                string error = string.Empty;

                switch (columnName)
                {
                    case nameof(Amount):
                        if (Amount <= 0)
                            error = "Kwota musi być większa od 0";
                        break;

                    case nameof(Description):
                        if (string.IsNullOrWhiteSpace(Description))
                            error = "Opis jest wymagany";
                        else if (Description.Length > 200)
                            error = "Opis nie może być dłuższy niż 200 znaków";
                        break;

                    case nameof(SelectedCategory):
                        if (SelectedCategory == null)
                            error = "Kategoria jest wymagana";
                        break;

                    case nameof(Date):
                        if (Date > DateTime.Today)
                            error = "Data nie może być z przyszłości";
                        else if (Date < DateTime.Today.AddYears(-10))
                            error = "Data nie może być starsza niż 10 lat";
                        break;
                }

                return error;
            }
        }

        private bool IsValid()
        {
            var properties = new[] { nameof(Amount), nameof(Description), nameof(SelectedCategory), nameof(Date) };
            var hasErrors = properties.Any(property => !string.IsNullOrEmpty(this[property]));
            
            if (hasErrors)
            {
                ValidationMessage = "Proszę poprawić błędy walidacji.";
                return false;
            }

            ValidationMessage = string.Empty;
            return true;
        }

        #endregion

        #region Helper Methods

        private void InitializeFromTransaction(Transaction transaction)
        {
            if (transaction != null)
            {
                Amount = transaction.Amount;
                Description = transaction.Description;
                Date = transaction.Date;
                IsIncome = transaction.IsIncome;
                SelectedCategory = Categories.FirstOrDefault(c => c.Id == transaction.CategoryId);
            }
            else
            {
                // Wartości domyślne dla nowej transakcji
                Amount = 0;
                Description = string.Empty;
                Date = DateTime.Today;
                IsIncome = false;
                SelectedCategory = null;
            }
        }

        private Transaction CreateTransactionFromInput()
        {
            return new Transaction
            {
                Amount = Amount,
                Description = Description?.Trim(),
                Date = Date,
                CategoryId = SelectedCategory.Id,
                Category = SelectedCategory,
                IsIncome = IsIncome,
                CreatedAt = IsEditMode ? _originalTransaction.CreatedAt : DateTime.Now
            };
        }

        #endregion

        #region Events

        public event EventHandler<Transaction> TransactionSaved;
        public event EventHandler TransactionCancelled;

        private void OnTransactionSaved(Transaction transaction)
        {
            TransactionSaved?.Invoke(this, transaction);
        }

        private void OnTransactionCancelled()
        {
            TransactionCancelled?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }
}