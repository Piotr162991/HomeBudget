using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using HouseBug.Models;
using HouseBug.Services;
using HouseBug.ViewModels.Base;

namespace HouseBug.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly BudgetManager _budgetManager;
        private readonly ReportGenerator _reportGenerator;

        public MainViewModel()
        {
            _budgetManager = new BudgetManager();
            _reportGenerator = new ReportGenerator(_budgetManager);
            
            InitializeCommands();
            LoadData();
        }

        #region Properties

        private ObservableCollection<Transaction> _transactions;
        public ObservableCollection<Transaction> Transactions
        {
            get => _transactions;
            set => SetProperty(ref _transactions, value);
        }

        private ObservableCollection<Category> _categories;
        public ObservableCollection<Category> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        private Transaction _selectedTransaction;
        public Transaction SelectedTransaction
        {
            get => _selectedTransaction;
            set => SetProperty(ref _selectedTransaction, value);
        }

        private BudgetSummary _currentMonthSummary;
        public BudgetSummary CurrentMonthSummary
        {
            get => _currentMonthSummary;
            set => SetProperty(ref _currentMonthSummary, value);
        }

        private DateTime _selectedDate = DateTime.Now;
        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (SetProperty(ref _selectedDate, value))
                {
                    LoadTransactionsForMonth();
                    LoadMonthlySummary();
                }
            }
        }

        private Category _selectedCategoryFilter;
        public Category SelectedCategoryFilter
        {
            get => _selectedCategoryFilter;
            set
            {
                if (SetProperty(ref _selectedCategoryFilter, value))
                {
                    FilterTransactions();
                }
            }
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterTransactions();
                }
            }
        }

        private decimal _totalIncome;
        public decimal TotalIncome
        {
            get => _totalIncome;
            set => SetProperty(ref _totalIncome, value);
        }

        private decimal _totalExpenses;
        public decimal TotalExpenses
        {
            get => _totalExpenses;
            set => SetProperty(ref _totalExpenses, value);
        }

        private decimal _balance;
        public decimal Balance
        {
            get => _balance;
            set => SetProperty(ref _balance, value);
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        #endregion

        #region Commands

        public ICommand AddTransactionCommand { get; private set; }
        public ICommand EditTransactionCommand { get; private set; }
        public ICommand DeleteTransactionCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }
        public ICommand ExportToCsvCommand { get; private set; }
        public ICommand GenerateReportCommand { get; private set; }
        public ICommand ClearFiltersCommand { get; private set; }
        public ICommand ShowPreviousMonthCommand { get; private set; }
        public ICommand ShowNextMonthCommand { get; private set; }

        private void InitializeCommands()
        {
            AddTransactionCommand = new RelayCommand(AddTransaction);
            EditTransactionCommand = new RelayCommand(EditTransaction, () => SelectedTransaction != null);
            DeleteTransactionCommand = new RelayCommand(DeleteTransaction, () => SelectedTransaction != null);
            RefreshCommand = new RelayCommand(RefreshData);
            ExportToCsvCommand = new RelayCommand(ExportToCsv, () => Transactions?.Any() == true);
            GenerateReportCommand = new RelayCommand(GenerateReport);
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            ShowPreviousMonthCommand = new RelayCommand(ShowPreviousMonth);
            ShowNextMonthCommand = new RelayCommand(ShowNextMonth);
        }

        #endregion

        #region Command Methods

        private void AddTransaction()
        {
            var transactionViewModel = new TransactionViewModel(_budgetManager, Categories?.ToList());
            
            // Tutaj powinna być logika otwierania okna dialogowego
            // Na razie używamy przykładowej implementacji
            var newTransaction = ShowTransactionDialog(transactionViewModel);
            
            if (newTransaction != null)
            {
                Transactions?.Add(newTransaction);
                UpdateSummary();
                StatusMessage = "Transakcja została dodana.";
            }
        }

        private void EditTransaction()
        {
            if (SelectedTransaction == null) return;

            var transactionViewModel = new TransactionViewModel(_budgetManager, Categories?.ToList(), SelectedTransaction);
            
            var updatedTransaction = ShowTransactionDialog(transactionViewModel);
            
            if (updatedTransaction != null)
            {
                var index = Transactions.IndexOf(SelectedTransaction);
                Transactions[index] = updatedTransaction;
                UpdateSummary();
                StatusMessage = "Transakcja została zaktualizowana.";
            }
        }

        private async void DeleteTransaction()
        {
            if (SelectedTransaction == null) return;

            // Tutaj powinna być logika potwierdzenia usunięcia
            var result = ShowConfirmationDialog("Czy na pewno chcesz usunąć tę transakcję?");
            
            if (result)
            {
                SetBusy(true, "Usuwanie transakcji...");
                
                try
                {
                    var success = await _budgetManager.DeleteTransactionAsync(SelectedTransaction.Id);
                    
                    if (success)
                    {
                        Transactions?.Remove(SelectedTransaction);
                        UpdateSummary();
                        StatusMessage = "Transakcja została usunięta.";
                    }
                    else
                    {
                        StatusMessage = "Błąd podczas usuwania transakcji.";
                    }
                }
                finally
                {
                    SetBusy(false);
                }
            }
        }

        private async void RefreshData()
        {
            SetBusy(true, "Odświeżanie danych...");
            
            try
            {
                await LoadDataAsync();
                StatusMessage = "Dane zostały odświeżone.";
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void ExportToCsv()
        {
            if (Transactions?.Any() != true) return;

            SetBusy(true, "Eksportowanie do CSV...");
            
            try
            {
                // Tutaj powinna być logika wyboru lokalizacji pliku
                var filePath = GetSaveFilePath("csv");
                
                if (!string.IsNullOrEmpty(filePath))
                {
                    var success = await _reportGenerator.ExportToCsvAsync(Transactions.ToList(), filePath);
                    
                    if (success)
                    {
                        StatusMessage = $"Dane wyeksportowane do: {filePath}";
                    }
                    else
                    {
                        StatusMessage = "Błąd podczas eksportowania danych.";
                    }
                }
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void GenerateReport()
        {
            SetBusy(true, "Generowanie raportu...");
            
            try
            {
                var report = _reportGenerator.GenerateMonthlyReport(SelectedDate);
                ShowReportDialog(report);
                StatusMessage = "Raport został wygenerowany.";
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void ClearFilters()
        {
            SelectedCategoryFilter = null;
            SearchText = string.Empty;
            StatusMessage = "Filtry zostały wyczyszczone.";
        }

        private void ShowPreviousMonth()
        {
            SelectedDate = SelectedDate.AddMonths(-1);
        }

        private void ShowNextMonth()
        {
            SelectedDate = SelectedDate.AddMonths(1);
        }

        #endregion

        #region Data Loading

        private async void LoadData()
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            SetBusy(true, "Ładowanie danych...");
            
            try
            {
                // Ładowanie kategorii
                var categories = await Task.Run(() => _budgetManager.GetAllCategories());
                Categories = new ObservableCollection<Category>(categories);

                // Ładowanie transakcji dla aktualnego miesiąca
                LoadTransactionsForMonth();
                
                // Ładowanie podsumowania miesięcznego
                LoadMonthlySummary();
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void LoadTransactionsForMonth()
        {
            var transactions = _budgetManager.GetTransactionsByMonth(SelectedDate);
            Transactions = new ObservableCollection<Transaction>(transactions);
            UpdateSummary();
        }

        private void LoadMonthlySummary()
        {
            CurrentMonthSummary = _budgetManager.GetMonthlySummary(SelectedDate);
        }

        private void FilterTransactions()
        {
            var allTransactions = _budgetManager.GetTransactionsByMonth(SelectedDate);
            
            // Filtrowanie według kategorii
            if (SelectedCategoryFilter != null)
            {
                allTransactions = allTransactions.Where(t => t.CategoryId == SelectedCategoryFilter.Id).ToList();
            }
            
            // Filtrowanie według tekstu wyszukiwania
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                allTransactions = allTransactions.Where(t => 
                    t.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    t.Category.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            
            Transactions = new ObservableCollection<Transaction>(allTransactions);
            UpdateSummary();
        }

        private void UpdateSummary()
        {
            if (Transactions == null) return;
            
            TotalIncome = Transactions.Where(t => t.IsIncome).Sum(t => t.Amount);
            TotalExpenses = Transactions.Where(t => !t.IsIncome).Sum(t => t.Amount);
            Balance = TotalIncome - TotalExpenses;
        }

        #endregion

        #region Helper Methods - Te metody powinny być zaimplementowane w warstwie widoku

        private Transaction ShowTransactionDialog(TransactionViewModel viewModel)
        {
            // Implementacja zależna od warstwy widoku
            // Zwraca nową/zaktualizowaną transakcję lub null jeśli anulowano
            throw new NotImplementedException("Metoda powinna być zaimplementowana w warstwie widoku");
        }

        private bool ShowConfirmationDialog(string message)
        {
            // Implementacja zależna od warstwy widoku
            throw new NotImplementedException("Metoda powinna być zaimplementowana w warstwie widoku");
        }

        private string GetSaveFilePath(string extension)
        {
            // Implementacja zależna od warstwy widoku
            throw new NotImplementedException("Metoda powinna być zaimplementowana w warstwie widoku");
        }

        private void ShowReportDialog(string report)
        {
            // Implementacja zależna od warstwy widoku
            throw new NotImplementedException("Metoda powinna być zaimplementowana w warstwie widoku");
        }

        #endregion

        #region Cleanup

        private bool _disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _budgetManager?.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}