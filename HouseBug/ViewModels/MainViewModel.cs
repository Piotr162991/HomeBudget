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
        private readonly BudgetManager _budgetManager;
        private readonly ReportGenerator _reportGenerator;

        public MainViewModel()
        {
            _budgetManager = new BudgetManager();
            _reportGenerator = new ReportGenerator(_budgetManager);

            Transactions = new ObservableCollection<Transaction>();
            Categories = new ObservableCollection<Category>();
            MonthlyBudgets = new ObservableCollection<MonthlyBudget>();
            FilteredTransactions = new List<Transaction>();
            _searchText = string.Empty;
            _statusMessage = string.Empty;

            InitializeCommands();
            _ = LoadDataAsync();
        }

        public event Func<TransactionViewModel, Transaction?>? ShowTransactionDialogRequested;
        public event Func<string, bool>? ShowConfirmationDialogRequested;
        public event Func<string, string>? GetSaveFilePathRequested;

        private ObservableCollection<Transaction> _transactions = null!;

        public ObservableCollection<Transaction> Transactions
        {
            get => _transactions;
            set => SetProperty(ref _transactions, value);
        }

        private ObservableCollection<Category> _categories;
        public ObservableCollection<Category> Categories
        {
            get => _categories;
            set
            {
                if (_categories != value)
                {
                    _categories = value;
                    OnPropertyChanged(nameof(Categories)); // Wywołanie zdarzenia PropertyChanged
                }
            }
        }

        public ObservableCollection<MonthlyBudget> MonthlyBudgets { get; }

        private MonthlyBudget? _selectedMonthlyBudget;
        public MonthlyBudget? SelectedMonthlyBudget
        {
            get => _selectedMonthlyBudget;
            set => SetProperty(ref _selectedMonthlyBudget, value);
        }

        private Transaction? _selectedTransaction;

        public Transaction? SelectedTransaction
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
                    RefreshDataForSelectedDateAsync().ConfigureAwait(false);
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
                    foreach (var t in Transactions?.ToList() ?? Enumerable.Empty<Transaction>())
                        t.ForceUpdateAmounts();
                    foreach (var b in MonthlyBudgets?.ToList() ?? Enumerable.Empty<MonthlyBudget>())
                        b.ForceUpdateAmounts();
                    RefreshDataForSelectedDateAsync().ConfigureAwait(false);
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
                RefreshData();
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
            _budgetManager.ClearChangeTracker();
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
            
            // Przywróć wszystkie transakcje
            Transactions.Clear();
            foreach (var transaction in _allTransactions.OrderByDescending(t => t.Date))
            {
                Transactions.Add(transaction);
            }
            
            FilteredTransactions = new List<Transaction>(_allTransactions);
            LoadMonthlySummary();
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

        private async Task LoadDataAsync()
        {
            SetBusy(true, "Ładowanie danych...");
            try
            {
                var settings = _budgetManager.GetAppSettings();
                DefaultCurrency = settings.DefaultCurrency;
                UpdateCurrencySymbol();

                Categories.Clear();
                Categories.Add(CreateAllCategoriesFilter());
                var categories = await _budgetManager.GetCategoriesAsync();
                foreach (var category in categories)
                {
                    Categories.Add(category);
                }

                await LoadTransactionsForMonthAsync();
                await LoadMonthlyBudgetsAsync();
                LoadMonthlySummary();
                
                StatusMessage = "Dane zostały załadowane";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd podczas ładowania danych: {ex.Message}";
            }
            finally
            {
                SetBusy(false);
            }
        }

        private Category CreateAllCategoriesFilter() => new Category
        {
            Id = -1,
            Name = "Wszystko",
            Description = "Pokaż transakcje ze wszystkich kategorii",
            Color = "#808080"
        };

        private async Task LoadTransactionsForMonthAsync()
        {
            try
            {
                var transactions = await Task.Run(() => _budgetManager.GetTransactionsByMonth(SelectedDate));
                UpdateTransactionsList(transactions);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd podczas ładowania transakcji: {ex.Message}";
            }
        }

        private List<Transaction> _allTransactions = new List<Transaction>();

        private void UpdateTransactionsList(List<Transaction> transactions, bool shouldFilter = true)
        {
            if (transactions == null) return;
            
            // Zachowaj kopię wszystkich transakcji przed filtrowaniem
            _allTransactions = new List<Transaction>(transactions);
            
            Transactions.Clear();
            foreach (var transaction in transactions.OrderByDescending(t => t.Date))
            {
                Transactions.Add(transaction);
            }

            if (shouldFilter)
            {
                FilterTransactions();
            }
        }

        private async Task RefreshDataForSelectedDateAsync()
        {
            SetBusy(true, "Ładowanie danych...");
            try
            {
                await LoadTransactionsForMonthAsync();
                LoadMonthlySummary();
                await LoadMonthlyBudgetsAsync();
                StatusMessage = "Dane zostały zaktualizowane";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd podczas ładowania danych: {ex.Message}";
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async Task LoadMonthlyBudgetsAsync()
        {
            try
            {
                MonthlyBudgets.Clear();
                var budgets = await Task.Run(() => 
                    _budgetManager.GetMonthlyBudgets(SelectedDate.Month, SelectedDate.Year));
                
                foreach (var budget in budgets)
                {
                    MonthlyBudgets.Add(budget);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd podczas ładowania budżetów: {ex.Message}";
            }
        }

        private void LoadMonthlySummary()
        {
            var filteredTransactions = FilteredTransactions;
            TotalIncome = filteredTransactions.Where(t => t.IsIncome).Sum(t => t.Amount);
            TotalExpenses = filteredTransactions.Where(t => !t.IsIncome).Sum(t => t.Amount);
            Balance = TotalIncome - TotalExpenses;
        }

        private void FilterTransactions()
        {
            var query = _allTransactions.AsEnumerable();

            if (SelectedCategoryFilter != null && SelectedCategoryFilter.Id != -1)
            {
                query = query.Where(t => t.CategoryId == SelectedCategoryFilter.Id);
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                query = query.Where(t =>
                    t.Description?.ToLower().Contains(searchLower) == true ||
                    t.Category?.Name?.ToLower().Contains(searchLower) == true ||
                    t.Amount.ToString().Contains(searchLower));
            }

            var filteredList = query.OrderByDescending(t => t.Date).ToList();
            FilteredTransactions = filteredList;

            // Aktualizacja widocznych transakcji
            Transactions.Clear();
            foreach (var transaction in filteredList)
            {
                Transactions.Add(transaction);
            }

            LoadMonthlySummary();
        }

        private List<Transaction> FilteredTransactions { get; set; } = new List<Transaction>();

        public async Task SaveMonthlyBudgetAsync(MonthlyBudget budget)
        {
            if (budget == null) return;

            SetBusy(true, "Zapisywanie budżetu...");
            try
            {
                await _budgetManager.SaveMonthlyBudgetAsync(budget);
                StatusMessage = "Budżet został zaktualizowany";
                await LoadMonthlyBudgetsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd podczas zapisywania budżetu: {ex.Message}";
            }
            finally
            {
                SetBusy(false);
            }
        }

        public async Task RefreshCurrencyFromSettingsAsync()
        {
            var settings = _budgetManager.GetAppSettings();
            DefaultCurrency = settings.DefaultCurrency;
            await RefreshDataForSelectedDateAsync();
        }

        public async Task HandleTransactionUpdateAsync(Transaction transaction)
        {
            if (transaction == null) return;

            SetBusy(true, "Aktualizowanie transakcji...");
            try
            {
                if (await _budgetManager.UpdateTransactionAsync(transaction))
                {
                    LoadMonthlySummary();
                    StatusMessage = "Transakcja została zaktualizowana";
                    await LoadMonthlyBudgetsAsync();
                }
                else
                {
                    StatusMessage = "Nie udało się zaktualizować transakcji";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd podczas aktualizacji transakcji: {ex.Message}";
            }
            finally
            {
                SetBusy(false);
            }
        }

        public AppSettings GetAppSettings()
        {
            return _budgetManager.GetAppSettings();
        }

        private void RefreshStatisticsPanel()
        {
            LoadMonthlySummary();
            FilterTransactions();
        }

        public async Task HandleAppSettingsUpdateAsync(AppSettings settings)
        {
            if (settings == null) return;

            SetBusy(true, "Aktualizowanie ustawień...");
            try
            {
                if (await _budgetManager.UpdateAppSettingsAsync(settings))
                {
                    await RefreshCurrencyFromSettingsAsync();
                    RefreshStatisticsPanel();
                    StatusMessage = "Ustawienia zostały zaktualizowane";
                }
                else
                {
                    StatusMessage = "Nie udało się zaktualizować ustawień";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"B��ąd podczas aktualizacji ustawień: {ex.Message}";
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void GenerateYearlyReport()
        {
            var filePath = GetSaveFilePathRequested?.Invoke("txt");
            if (string.IsNullOrEmpty(filePath)) return;

            SetBusy(true, "Generowanie raportu rocznego...");
            try
            {
                var report = _reportGenerator.GenerateYearlyReport(SelectedDate.Year);
                await _reportGenerator.SaveReportToFileAsync(report, filePath);
                StatusMessage = $"Raport roczny został zapisany do: {filePath}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd podczas generowania raportu: {ex.Message}";
            }
            finally
            {
                SetBusy(false);
            }
        }

        public new void Dispose()
        {
            base.Dispose();
            _budgetManager?.Dispose();
        }
    }
}
