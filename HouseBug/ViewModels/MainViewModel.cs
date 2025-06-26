using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using HouseBug.Models;
using HouseBug.Services;
using HouseBug.ViewModels.Base;
using HouseBug.Views.Dialogs;

namespace HouseBug.ViewModels
{
    public class MainViewModel : ViewModelBase, IDisposable
    {
        public readonly BudgetManager _budgetManager;  // Zmieniono z private na public
        private readonly ReportGenerator _reportGenerator;

        public MainViewModel()
        {
            _budgetManager = new BudgetManager();
            _reportGenerator = new ReportGenerator(new BudgetManager());

            Transactions = new ObservableCollection<Transaction>();
            Categories = new ObservableCollection<Category>();
            MonthlyBudgets = new ObservableCollection<MonthlyBudget>();

            InitializeCommands();
            LoadData();
        }

        public event Func<TransactionViewModel, Transaction?> ShowTransactionDialogRequested;
        public event Func<string, bool> ShowConfirmationDialogRequested;
        public event Func<string, string> GetSaveFilePathRequested;

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

        public ObservableCollection<MonthlyBudget> MonthlyBudgets { get; set; } = new ObservableCollection<MonthlyBudget>();

        private MonthlyBudget _selectedMonthlyBudget;
        public MonthlyBudget SelectedMonthlyBudget
        {
            get => _selectedMonthlyBudget;
            set => SetProperty(ref _selectedMonthlyBudget, value);
        }

        private Transaction _selectedTransaction;

        public Transaction SelectedTransaction
        {
            get => _selectedTransaction;
            set => SetProperty(ref _selectedTransaction, value);
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
                    LoadMonthlyBudgetsAsync();
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

        private string _currencySymbol = "zł";
        public string CurrencySymbol
        {
            get => _currencySymbol;
            set => SetProperty(ref _currencySymbol, value);
        }

        private string _defaultCurrency = "PLN";
        public string DefaultCurrency
        {
            get => _defaultCurrency;
            set
            {
                if (SetProperty(ref _defaultCurrency, value))
                {
                    UpdateCurrencySymbol();
                    OnPropertyChanged(nameof(CurrencySymbol));
                    OnPropertyChanged(nameof(TotalIncome));
                    OnPropertyChanged(nameof(TotalExpenses));
                    OnPropertyChanged(nameof(Balance));
                    // Wymuś powiadomienie o zmianie Amount w każdej transakcji
                    foreach (var t in Transactions?.ToList() ?? Enumerable.Empty<Transaction>())
                        t.ForceUpdateAmounts();
                    // Wymuś powiadomienie o zmianie kwot w każdym budżecie
                    foreach (var b in MonthlyBudgets?.ToList() ?? Enumerable.Empty<MonthlyBudget>())
                        b.ForceUpdateAmounts();
                    LoadTransactionsForMonth();
                    LoadMonthlySummary();
                    LoadMonthlyBudgetsAsync();
                }
            }
        }

        private void UpdateCurrencySymbol()
        {
            switch (DefaultCurrency)
            {
                case "PLN": CurrencySymbol = "zł"; break;
                case "USD": CurrencySymbol = "$"; break;
                case "GBP": CurrencySymbol = "£"; break;
                default: CurrencySymbol = DefaultCurrency; break;
            }
        }

        public ICommand AddTransactionCommand { get; private set; }
        public ICommand EditTransactionCommand { get; private set; }
        public ICommand DeleteTransactionCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }
        public ICommand ExportToCsvCommand { get; private set; }
        public ICommand GenerateReportCommand { get; private set; }
        public ICommand ClearFiltersCommand { get; private set; }
        public ICommand ShowPreviousMonthCommand { get; private set; }
        public ICommand ShowNextMonthCommand { get; private set; }
        public ICommand GenerateYearlyReportCommand { get; private set; }


        private void InitializeCommands()
        {
            AddTransactionCommand = new RelayCommand(AddTransaction);
            EditTransactionCommand = new RelayCommand(EditTransaction, () => SelectedTransaction != null);
            DeleteTransactionCommand = new RelayCommand(DeleteTransaction, () => SelectedTransaction != null);
            RefreshCommand = new RelayCommand(RefreshData);
            ExportToCsvCommand = new RelayCommand(ExportToCsv);
            GenerateReportCommand = new RelayCommand(GenerateReport);
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            ShowPreviousMonthCommand = new RelayCommand(ShowPreviousMonth);
            ShowNextMonthCommand = new RelayCommand(ShowNextMonth);
            GenerateYearlyReportCommand = new RelayCommand(GenerateYearlyReport);
        }

        private void AddTransaction()
        {
            var transactionViewModel = new TransactionViewModel(_budgetManager, Categories?.ToList());

            var newTransaction = ShowTransactionDialogRequested?.Invoke(transactionViewModel);

            if (newTransaction != null)
            {
                Transactions?.Add(newTransaction);
                LoadMonthlySummary();
                StatusMessage = "Dodano nową transakcję";
            }
        }

        private void EditTransaction()
        {
            if (SelectedTransaction == null) return;

            var transactionViewModel =
                new TransactionViewModel(_budgetManager, Categories?.ToList(), SelectedTransaction);

            var updatedTransaction = ShowTransactionDialogRequested?.Invoke(transactionViewModel);

            if (updatedTransaction != null)
            {
                var index = Transactions.IndexOf(SelectedTransaction);
                if (index >= 0)
                {
                    Transactions[index] = updatedTransaction;
                    LoadMonthlySummary();
                    StatusMessage = "Zaktualizowano transakcję";
                }
            }
        }

        private async void DeleteTransaction()
        {
            if (SelectedTransaction == null) return;

            var confirmed = ShowConfirmationDialogRequested?.Invoke("Czy na pewno chcesz usunąć tę transakcję?") ??
                            false;

            if (confirmed)
            {
                SetBusy(true, "Usuwanie transakcji...");

                var success = await _budgetManager.DeleteTransactionAsync(SelectedTransaction.Id);

                if (success)
                {
                    Transactions.Remove(SelectedTransaction);
                    LoadMonthlySummary();
                    StatusMessage = "Transakcja została usunięta";
                }
                else
                {
                    StatusMessage = "Błąd podczas usuwania transakcji";
                }

                SetBusy(false);
            }
        }

        private async void RefreshData()
        {
            SetBusy(true, "Odświeżanie danych...");
            await LoadDataAsync();
            SetBusy(false);
            StatusMessage = "Dane zostały odświeżone";
        }

        private async void ExportToCsv()
        {
            var filePath = GetSaveFilePathRequested?.Invoke("csv");
            if (!string.IsNullOrEmpty(filePath))
            {
                SetBusy(true, "Eksportowanie...");

                try
                {
                    await ExportTransactionsToCsvAsync(Transactions.ToList(), filePath);
                    StatusMessage = $"Dane wyeksportowane do: {filePath}";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Błąd podczas eksportu: {ex.Message}";
                }

                SetBusy(false);
            }
        }

        private async Task ExportTransactionsToCsvAsync(List<Transaction> transactions, string filePath)
        {
            await Task.Run(() =>
            {
                using var writer = new System.IO.StreamWriter(filePath);

                writer.WriteLine("Data,Kategoria,Opis,Kwota,Typ");

                foreach (var transaction in transactions)
                {
                    var line = $"{transaction.Date:yyyy-MM-dd}," +
                               $"{transaction.Category?.Name ?? "Brak"}," +
                               $"\"{transaction.Description}\"," +
                               $"{transaction.Amount:F2}," +
                               $"{(transaction.IsIncome ? "Przychód" : "Wydatek")}";

                    writer.WriteLine(line);
                }
            });
        }

        private void GenerateReport()
        {
            var reportDialog = new Views.Dialogs.ReportDialog(_reportGenerator, _budgetManager)
            {
                Owner = Application.Current.MainWindow
            };

            var result = reportDialog.ShowDialog();

            if (result == true)
            {
                StatusMessage = "Raport został wygenerowany i zapisany do pliku.";
            }
        }


        private void ClearFilters()
        {
            SelectedCategoryFilter = null;
            SearchText = string.Empty;
            StatusMessage = "Filtry zostały wyczyszczone";
        }

        private void ShowPreviousMonth()
        {
            SelectedDate = SelectedDate.AddMonths(-1);
        }

        private void ShowNextMonth()
        {
            SelectedDate = SelectedDate.AddMonths(1);
        }

        private async void LoadData()
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            Categories.Clear();
            foreach (var cat in await _budgetManager.GetCategoriesAsync())
                Categories.Add(cat);
            LoadTransactionsForMonth();
            LoadMonthlySummary();
            await LoadMonthlyBudgetsAsync();

            var settings = _budgetManager.GetAppSettings();
            DefaultCurrency = settings.DefaultCurrency;
            UpdateCurrencySymbol();
        }

        private async Task LoadMonthlyBudgetsAsync()
        {
            MonthlyBudgets.Clear();
            var budgets = await _budgetManager.GetMonthlyBudgetsAsync(SelectedDate.Month, SelectedDate.Year);
            foreach (var b in budgets)
                MonthlyBudgets.Add(b);
        }

        private void LoadTransactionsForMonth()
        {
            var transactions = _budgetManager.GetTransactionsByMonth(SelectedDate);
            Transactions.Clear();
            foreach (var transaction in transactions)
            {
                Transactions.Add(transaction);
            }

            FilterTransactions();
        }

        public void LoadMonthlySummary()
        {
            UpdateSummary();
        }

        private void FilterTransactions()
        {
            var allTransactions = _budgetManager.GetTransactionsByMonth(SelectedDate);
            var filteredTransactions = allTransactions.AsEnumerable();

            if (SelectedCategoryFilter != null)
            {
                filteredTransactions = filteredTransactions.Where(t => t.CategoryId == SelectedCategoryFilter.Id);
            }

            if (!string.IsNullOrEmpty(SearchText))
            {
                filteredTransactions = filteredTransactions.Where(t =>
                    t.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    t.Category.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            Transactions.Clear();
            foreach (var transaction in filteredTransactions)
            {
                Transactions.Add(transaction);
            }

            UpdateSummary();
        }

        private void UpdateSummary()
        {
            var currentTransactions = Transactions.ToList();
            TotalIncome = currentTransactions.Where(t => t.IsIncome).Sum(t => t.Amount);
            TotalExpenses = currentTransactions.Where(t => !t.IsIncome).Sum(t => t.Amount);
            Balance = TotalIncome - TotalExpenses;
        }

        public void Dispose()
        {
            _budgetManager?.Dispose();
        }
        
        private void GenerateYearlyReport()
        {
            var reportDialog = new Views.Dialogs.ReportDialog(_reportGenerator, _budgetManager)
            {
                Owner = Application.Current.MainWindow
            };
            var result = reportDialog.ShowDialog();

            if (result == true)
            {
                StatusMessage = "Raport roczny został wygenerowany i zapisany do pliku.";
            }
        }

        public async void SaveMonthlyBudget(MonthlyBudget budget)
        {
            await _budgetManager.SaveMonthlyBudgetAsync(budget);
            await LoadMonthlyBudgetsAsync();
        }

        public async void RefreshCurrencyFromSettings()
        {
            var settings = _budgetManager.GetAppSettings();
            DefaultCurrency = settings.DefaultCurrency;
            UpdateCurrencySymbol();
        }

        public void RefreshStatisticsPanel()
        {
            OnPropertyChanged(nameof(TotalIncome));
            OnPropertyChanged(nameof(TotalExpenses));
            OnPropertyChanged(nameof(Balance));
        }
    }
}
