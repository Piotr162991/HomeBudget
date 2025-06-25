using HouseBug.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseBug.Services
{
    public class ReportGenerator
    {
        private readonly BudgetManager _budgetManager;

        public ReportGenerator(BudgetManager budgetManager)
        {
            _budgetManager = budgetManager;
        }

        public string GenerateMonthlyReport(DateTime month)
        {
            var summary = _budgetManager.GetMonthlySummary(month);
            var transactions = _budgetManager.GetTransactionsByMonth(month);

            var report = new StringBuilder();
            report.AppendLine($"=== RAPORT MIESIĘCZNY - {summary.FormattedPeriod.ToUpper()} ===");
            report.AppendLine();

            report.AppendLine("PODSUMOWANIE FINANSOWE:");
            report.AppendLine($"Przychody:     {summary.TotalIncome:C}");
            report.AppendLine($"Wydatki:       {summary.TotalExpenses:C}");
            report.AppendLine($"Saldo:         {summary.Balance:C}");
            report.AppendLine($"Status:        {summary.BalanceStatus}");
            report.AppendLine();

            if (summary.CategorySummaries.Any())
            {
                report.AppendLine("WYDATKI WEDŁUG KATEGORII:");
                foreach (var category in summary.CategorySummaries.OrderByDescending(c => c.Amount))
                {
                    report.AppendLine(
                        $"{category.CategoryName,-20} {category.Amount,10:C} ({category.Percentage,5:F1}%)");
                }

                report.AppendLine();
            }

            report.AppendLine("LISTA TRANSAKCJI:");
            report.AppendLine($"{"Data",-12} {"Kategoria",-15} {"Opis",-30} {"Kwota",10}");
            report.AppendLine(new string('-', 70));

            foreach (var transaction in transactions.OrderBy(t => t.Date))
            {
                var amount = transaction.IsIncome ? $"+{transaction.Amount:C}" : $"-{transaction.Amount:C}";
                report.AppendLine(
                    $"{transaction.Date:dd.MM.yyyy,-12} {transaction.Category.Name,-15} {transaction.Description,-30} {amount,10}");
            }

            return report.ToString();
        }

        public async Task<bool> ExportToCsvAsync(List<Transaction> transactions, string filePath)
        {
            try
            {
                using var writer = new StreamWriter(filePath, false, Encoding.UTF8);

                await writer.WriteLineAsync("Data,Kategoria,Opis,Kwota,Typ");

                foreach (var transaction in transactions)
                {
                    var line = $"{transaction.Date:yyyy-MM-dd}," +
                               $"\"{transaction.Category.Name}\"," +
                               $"\"{transaction.Description}\"," +
                               $"{transaction.Amount}," +
                               $"{(transaction.IsIncome ? "Przychód" : "Wydatek")}";

                    await writer.WriteLineAsync(line);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public string GenerateYearlyReport(int year)
        {
            var yearlySummary = _budgetManager.GetYearlySummary(year);

            var report = new StringBuilder();
            report.AppendLine($"=== RAPORT ROCZNY - {year} ===");
            report.AppendLine();

            var totalIncome = yearlySummary.Sum(s => s.TotalIncome);
            var totalExpenses = yearlySummary.Sum(s => s.TotalExpenses);
            var totalBalance = totalIncome - totalExpenses;

            report.AppendLine("PODSUMOWANIE ROCZNE:");
            report.AppendLine($"Łączne przychody:  {totalIncome:C}");
            report.AppendLine($"Łączne wydatki:    {totalExpenses:C}");
            report.AppendLine($"Saldo roczne:      {totalBalance:C}");
            report.AppendLine();

            report.AppendLine("PODSUMOWANIE MIESIĘCZNE:");
            report.AppendLine($"{"Miesiąc",-15} {"Przychody",12} {"Wydatki",12} {"Saldo",12}");
            report.AppendLine(new string('-', 55));

            foreach (var monthlySummary in yearlySummary)
            {
                report.AppendLine($"{monthlySummary.FormattedPeriod,-15} " +
                                  $"{monthlySummary.TotalIncome,12:C} " +
                                  $"{monthlySummary.TotalExpenses,12:C} " +
                                  $"{monthlySummary.Balance,12:C}");
            }

            var allCategories = yearlySummary
                .SelectMany(s => s.CategorySummaries)
                .GroupBy(cs => cs.CategoryName)
                .Select(g => new CategorySummary
                {
                    CategoryName = g.Key,
                    Amount = g.Sum(cs => cs.Amount),
                    TransactionCount = g.Sum(cs => cs.TransactionCount)
                })
                .OrderByDescending(cs => cs.Amount)
                .Take(10);

            report.AppendLine();
            report.AppendLine("TOP 10 KATEGORII WYDATKÓW:");
            report.AppendLine($"{"Kategoria",-20} {"Kwota",12} {"Transakcje",12}");
            report.AppendLine(new string('-', 48));

            foreach (var category in allCategories)
            {
                report.AppendLine($"{category.CategoryName,-20} {category.Amount,12:C} {category.TransactionCount,12}");
            }

            return report.ToString();
        }

        public async Task<bool> SaveReportToFileAsync(string report, string filePath)
        {
            try
            {
                await File.WriteAllTextAsync(filePath, report, Encoding.UTF8);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public Dictionary<string, object> GenerateStatistics(DateTime startDate, DateTime endDate)
        {
            var transactions = _budgetManager.GetTransactionsByDateRange(startDate, endDate);
            var income = transactions.Where(t => t.IsIncome).Sum(t => t.Amount);
            var expenses = transactions.Where(t => !t.IsIncome).Sum(t => t.Amount);

            return new Dictionary<string, object>
            {
                ["Okres"] = $"{startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}",
                ["Liczba transakcji"] = transactions.Count,
                ["Łączne przychody"] = income,
                ["Łączne wydatki"] = expenses,
                ["Saldo"] = income - expenses,
                ["Średni dzienny wydatek"] = expenses / Math.Max(1, (endDate - startDate).Days),
                ["Największy wydatek"] = transactions.Where(t => !t.IsIncome).DefaultIfEmpty().Max(t => t?.Amount ?? 0),
                ["Największy przychód"] = transactions.Where(t => t.IsIncome).DefaultIfEmpty().Max(t => t?.Amount ?? 0),
                ["Najczęstsza kategoria"] = transactions.GroupBy(t => t.Category.Name).OrderByDescending(g => g.Count())
                    .FirstOrDefault()?.Key ?? "Brak"
            };
        }
    }
}