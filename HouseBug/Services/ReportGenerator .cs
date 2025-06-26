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
            var transactions = _budgetManager.GetTransactionsByMonth(month);
            var income = transactions.Where(t => t.IsIncome).Sum(t => t.Amount);
            var expenses = transactions.Where(t => !t.IsIncome).Sum(t => t.Amount);
            var balance = income - expenses;

            var categoryGroups = transactions
                .Where(t => !t.IsIncome)
                .GroupBy(t => t.Category)
                .Select(g => new CategorySummary
                {
                    CategoryName = g.Key.Name,
                    Amount = g.Sum(t => t.Amount),
                    TransactionCount = g.Count(),
                    Percentage = expenses > 0 ? (double)(g.Sum(t => t.Amount) / expenses) * 100 : 0,
                    Color = g.Key.Color
                })
                .OrderByDescending(cs => cs.Amount)
                .ToList();

            var report = new StringBuilder();
            report.AppendLine($"=== RAPORT MIESIĘCZNY - {month:MMMM yyyy} ===");
            report.AppendLine();

            report.AppendLine("PODSUMOWANIE FINANSOWE:");
            report.AppendLine($"Przychody:     {income:C}");
            report.AppendLine($"Wydatki:       {expenses:C}");
            report.AppendLine($"Saldo:         {balance:C}");
            report.AppendLine($"Status:        {(balance >= 0 ? "Pozytywny" : "Negatywny")}");
            report.AppendLine();

            if (categoryGroups.Any())
            {
                report.AppendLine("WYDATKI WEDŁUG KATEGORII:");
                foreach (var category in categoryGroups)
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
            var report = new StringBuilder();
            report.AppendLine($"=== RAPORT ROCZNY - {year} ===");
            report.AppendLine();

            decimal totalIncome = 0;
            decimal totalExpenses = 0;
            var monthlySummaries = new List<(DateTime Period, decimal Income, decimal Expenses, List<CategorySummary> Categories)>();

            for (int month = 1; month <= 12; month++)
            {
                var date = new DateTime(year, month, 1);
                var transactions = _budgetManager.GetTransactionsByMonth(date);
                
                var monthIncome = transactions.Where(t => t.IsIncome).Sum(t => t.Amount);
                var monthExpenses = transactions.Where(t => !t.IsIncome).Sum(t => t.Amount);
                
                var categorySummaries = transactions
                    .Where(t => !t.IsIncome)
                    .GroupBy(t => t.Category)
                    .Select(g => new CategorySummary
                    {
                        CategoryName = g.Key.Name,
                        Amount = g.Sum(t => t.Amount),
                        TransactionCount = g.Count(),
                        Color = g.Key.Color
                    })
                    .ToList();

                totalIncome += monthIncome;
                totalExpenses += monthExpenses;
                
                monthlySummaries.Add((date, monthIncome, monthExpenses, categorySummaries));
            }

            var totalBalance = totalIncome - totalExpenses;

            report.AppendLine("PODSUMOWANIE ROCZNE:");
            report.AppendLine($"Łączne przychody:  {totalIncome:C}");
            report.AppendLine($"Łączne wydatki:    {totalExpenses:C}");
            report.AppendLine($"Saldo roczne:      {totalBalance:C}");
            report.AppendLine();

            report.AppendLine("PODSUMOWANIE MIESIĘCZNE:");
            report.AppendLine($"{"Miesiąc",-15} {"Przychody",12} {"Wydatki",12} {"Saldo",12}");
            report.AppendLine(new string('-', 55));

            foreach (var summary in monthlySummaries)
            {
                var balance = summary.Income - summary.Expenses;
                report.AppendLine($"{summary.Period:MMMM yyyy,-15} " +
                                $"{summary.Income,12:C} " +
                                $"{summary.Expenses,12:C} " +
                                $"{balance,12:C}");
            }

            var yearlyCategories = monthlySummaries
                .SelectMany(s => s.Categories)
                .GroupBy(c => c.CategoryName)
                .Select(g => new CategorySummary
                {
                    CategoryName = g.Key,
                    Amount = g.Sum(c => c.Amount),
                    TransactionCount = g.Sum(c => c.TransactionCount)
                })
                .OrderByDescending(c => c.Amount)
                .Take(10);

            report.AppendLine();
            report.AppendLine("TOP 10 KATEGORII WYDATKÓW:");
            report.AppendLine($"{"Kategoria",-20} {"Kwota",12} {"Transakcje",12}");
            report.AppendLine(new string('-', 48));

            foreach (var category in yearlyCategories)
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
        
    }
}