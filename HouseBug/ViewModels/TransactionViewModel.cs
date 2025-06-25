using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using HouseBug.Models;
using HouseBug.Services;
using HouseBug.ViewModels.Base;

namespace HouseBug.ViewModels
{
    public class TransactionViewModel : CrudViewModelBase<Transaction>
    {
        private readonly BudgetManager _budgetManager;
        private readonly Transaction _originalTransaction;

        public TransactionViewModel(BudgetManager budgetManager, List<Category> categories, Transaction transaction = null)
        {
            _budgetManager = budgetManager;
            Categories = categories ?? new List<Category>();
            _originalTransaction = transaction;
            _isEditMode = transaction != null;

            InitializeCommands();
            PopulateFormFromItem(transaction);
        }

        #region Properties

        public List<Category> Categories { get; }

        private decimal _amount;
        public decimal Amount
        {
            get => _amount;
            set => SetProperty(ref _amount, value);
        }

        private string _description;
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

        public string WindowTitle => IsEditMode ? "Edytuj transakcję" : "Dodaj transakcję";

        #endregion

        #region Commands

        public ICommand SaveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public ICommand SetIncomeCommand { get; private set; }
        public ICommand SetExpenseCommand { get; private set; }

        private void InitializeCommands()
        {
            SaveCommand = CommandFactory.Create(Save, () => !IsBusy && IsValid());
            CancelCommand = CommandFactory.Create(Cancel);
            SetIncomeCommand = CommandFactory.Create(() => IsIncome = true);
            SetExpenseCommand = CommandFactory.Create(() => IsIncome = false);
        }

        #endregion

        #region Command Methods

        private async void Save()
        {
            if (!IsValid()) return;

            await HandleOperationAsync(IsEditMode ? "aktualizacja transakcji" : "zapisywanie transakcji", async () =>
            {
                var transaction = CreateItemFromInput();

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
            });
        }

        private void Cancel()
        {
            OnTransactionCancelled();
        }

        #endregion

        #region Public Methods

        public Transaction GetTransaction()
        {
            if (!IsValid())
                return null;

            return CreateItemFromInput();
        }

        #endregion

        #region Validation

        protected override string[] GetValidatableProperties()
        {
            return new[] { nameof(Amount), nameof(Description), nameof(SelectedCategory), nameof(Date) };
        }

        protected override string GetValidationError(string propertyName)
        {
            string error = string.Empty;

            switch (propertyName)
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

        protected override bool IsValid()
        {
            var isValid = base.IsValid();

            if (!isValid)
            {
                ValidationMessage = "Proszę poprawić błędy walidacji.";
                return false;
            }

            ValidationMessage = string.Empty;
            return true;
        }

        #endregion

        #region Override Methods

        protected override void PopulateFormFromItem(Transaction transaction)
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

        protected override void ClearForm()
        {
            Amount = 0;
            Description = string.Empty;
            Date = DateTime.Today;
            IsIncome = false;
            SelectedCategory = null;
        }

        protected override Transaction CreateItemFromInput()
        {
            return new Transaction
            {
                Amount = Amount,
                Description = Description?.Trim(),
                Date = Date,
                CategoryId = SelectedCategory.Id,
                Category = SelectedCategory,
                IsIncome = IsIncome,
                CreatedAt = IsEditMode ? _originalTransaction.CreatedAt : DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }

        protected override async Task<bool> SaveItemAsync(Transaction item)
        {
            if (item.Id > 0)
            {
                return await _budgetManager.UpdateTransactionAsync(item);
            }
            else
            {
                var savedItem = await _budgetManager.AddTransactionAsync(item);
                return savedItem != null;
            }
        }

        protected override async Task<bool> DeleteItemAsync(Transaction item)
        {
            return await _budgetManager.DeleteTransactionAsync(item.Id);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _budgetManager?.Dispose();
            }
            base.Dispose(disposing);
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