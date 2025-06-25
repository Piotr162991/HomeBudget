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

namespace HouseBug.ViewModels
{
    public class MainViewModel : ViewModelBase, IDisposable
    {
        private readonly BudgetManager _budgetManager;
        private readonly ReportGenerator _reportGenerator;

        public MainViewModel()
        {
            _budgetManager = new BudgetManager();
            _reportGenerator = new ReportGenerator(new BudgetManager());

            Transactions = new ObservableCollection<Transaction>();
            Categories = new ObservableCollection<Category>();

            InitializeCommands();
            LoadData();
        }

        // Event zamiast metody
        public event Func<TransactionViewModel, Transaction?> ShowTransactionDialogRequested;
        public event Func<string, bool> ShowConfirmationDialogRequested;
        public event Func<string, string> GetSaveFilePathRequested;

        // Properties (pozostaw bez zmian)
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

        // Commands
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
                    // Prosty eksport do CSV
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

                // Nagłówki
                writer.WriteLine("Data,Kategoria,Opis,Kwota,Typ");

                // Dane
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
            var report = GenerateSimpleReport();

            // Pokaż raport w prostym MessageBox lub stwórz prostą implementację
            var confirmed =
                ShowConfirmationDialogRequested?.Invoke(
                    $"Raport miesięczny:\n\n{report}\n\nCzy chcesz zapisać raport do pliku?") ?? false;

            if (confirmed)
            {
                var filePath = GetSaveFilePathRequested?.Invoke("txt");
                if (!string.IsNullOrEmpty(filePath))
                {
                    System.IO.File.WriteAllText(filePath, report);
                    StatusMessage = $"Raport zapisany do: {filePath}";
                }
            }
        }

        private string GenerateSimpleReport()
        {
            var monthName = SelectedDate.ToString("MMMM yyyy");
            var report = $"RAPORT MIESIĘCZNY - {monthName.ToUpper()}\n";
            report += new string('=', 50) + "\n\n";

            report += $"Przychody: {TotalIncome:C}\n";
            report += $"Wydatki: {TotalExpenses:C}\n";
            report += $"Saldo: {Balance:C}\n\n";

            if (Categories?.Any() == true)
            {
                report += "WYDATKI WEDŁUG KATEGORII:\n";
                report += new string('-', 30) + "\n";

                foreach (var category in Categories)
                {
                    var categoryExpenses = Transactions
                        .Where(t => !t.IsIncome && t.CategoryId == category.Id)
                        .Sum(t => t.Amount);

                    if (categoryExpenses > 0)
                    {
                        report += $"{category.Name}: {categoryExpenses:C}\n";
                    }
                }
            }

            report += $"\nRaport wygenerowany: {DateTime.Now:dd.MM.yyyy HH:mm}";

            return report;
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
            SetBusy(true, "Ładowanie danych...");
            await LoadDataAsync();
            SetBusy(false);
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var categories = _budgetManager.GetAllCategories();
                Categories.Clear();
                foreach (var category in categories)
                {
                    Categories.Add(category);
                }

                LoadTransactionsForMonth();
                LoadMonthlySummary();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd podczas ładowania danych: {ex.Message}";
            }
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

        private void LoadMonthlySummary()
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
            int year = DateTime.Now.Year;

            var result = MessageBox.Show(
                $"Wygenerować raport roczny za rok {year}?",
                "Generuj raport roczny",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                StatusMessage = "Anulowano generowanie raportu rocznego.";
                return;
            }

            string report = _reportGenerator.GenerateYearlyReport(year);

            var sfd = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Zapisz raport roczny",
                FileName = $"RaportRoczny_{year}.txt",
                Filter = "Plik tekstowy (*.txt)|*.txt|Wszystkie pliki (*.*)|*.*"
            };

            if (sfd.ShowDialog() == true)
            {
                File.WriteAllText(sfd.FileName, report, Encoding.UTF8);
                StatusMessage = $"Raport roczny za {year} został zapisany.";
            }
            else
            {
                StatusMessage = "Anulowano zapis raportu rocznego.";
            }
        }


    }
}