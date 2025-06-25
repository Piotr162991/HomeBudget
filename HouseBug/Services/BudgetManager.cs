using Microsoft.EntityFrameworkCore;
using HouseBug.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HouseBug.Services
{
    public class BudgetManager : IDisposable
    {
        private readonly BudgetContext _context;

        public BudgetManager()
        {
            _context = new BudgetContext();
        }

        #region Transaction Management

        public async Task<Transaction> AddTransactionAsync(Transaction transaction)
        {
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }


        public async Task<bool> UpdateTransactionAsync(Transaction transaction)
        {
            try
            {
                _context.Transactions.Update(transaction);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteTransactionAsync(int transactionId)
        {
            try
            {
                var transaction = await _context.Transactions.FindAsync(transactionId);
                if (transaction != null)
                {
                    _context.Transactions.Remove(transaction);
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public List<Transaction> GetTransactionsByDateRange(DateTime startDate, DateTime endDate)
        {
            return _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.Date >= startDate && t.Date <= endDate)
                .OrderByDescending(t => t.Date)
                .ToList();
        }

        public List<Transaction> GetTransactionsByMonth(DateTime date)
        {
            var startDate = new DateTime(date.Year, date.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);
            
            return GetTransactionsByDateRange(startDate, endDate);
        }

        public List<Transaction> GetTransactionsByCategory(int categoryId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.CategoryId == categoryId);

            if (startDate.HasValue)
                query = query.Where(t => t.Date >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(t => t.Date <= endDate.Value);

            return query.OrderByDescending(t => t.Date).ToList();
        }

        public async Task<List<Transaction>> GetRecentTransactionsAsync(int count = 10)
        {
            return await _context.Transactions
                .Include(t => t.Category)
                .OrderByDescending(t => t.Date)
                .Take(count)
                .ToListAsync();
        }

        #endregion

        #region Category Management

        public async Task<Category> AddCategoryAsync(Category category)
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public Category AddCategory(Category category)
        {
            _context.Categories.Add(category);
            _context.SaveChanges();
            return category;
        }

        public List<Category> GetAllCategories()
        {
            return _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToList();
        }

        public async Task<bool> UpdateCategoryAsync(Category category)
        {
            try
            {
                _context.Categories.Update(category);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteCategoryAsync(int categoryId)
        {
            try
            {
                var category = await _context.Categories.FindAsync(categoryId);
                if (category != null)
                {
                    var hasTransactions = await _context.Transactions
                        .AnyAsync(t => t.CategoryId == categoryId);

                    if (hasTransactions)
                    {
                        category.IsActive = false;
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        _context.Categories.Remove(category);
                        await _context.SaveChangesAsync();
                    }
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Budget Analysis

        public decimal GetMonthlyBalance(DateTime date)
        {
            var transactions = GetTransactionsByMonth(date);
            var income = transactions.Where(t => t.IsIncome).Sum(t => t.Amount);
            var expenses = transactions.Where(t => !t.IsIncome).Sum(t => t.Amount);
            return income - expenses;
        }

        public BudgetSummary GetMonthlySummary(DateTime date)
        {
            var transactions = GetTransactionsByMonth(date);
            var income = transactions.Where(t => t.IsIncome).Sum(t => t.Amount);
            var expenses = transactions.Where(t => !t.IsIncome).Sum(t => t.Amount);

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

            return new BudgetSummary
            {
                Period = date,
                TotalIncome = income,
                TotalExpenses = expenses,
                CategorySummaries = categoryGroups,
                TransactionCount = transactions.Count
            };
        }

        public List<BudgetSummary> GetYearlySummary(int year)
        {
            var summaries = new List<BudgetSummary>();

            for (int month = 1; month <= 12; month++)
            {
                var date = new DateTime(year, month, 1);
                summaries.Add(GetMonthlySummary(date));
            }

            return summaries;
        }

        public Dictionary<string, decimal> GetCategoryTotals(DateTime startDate, DateTime endDate)
        {
            return _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.Date >= startDate && t.Date <= endDate && !t.IsIncome)
                .GroupBy(t => t.Category.Name)
                .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));
        }

        #endregion

        #region Monthly Budget Management

        public async Task<MonthlyBudget> SetMonthlyBudgetAsync(int categoryId, int month, int year, decimal amount)
        {
            var existingBudget = await _context.MonthlyBudgets
                .FirstOrDefaultAsync(mb => mb.CategoryId == categoryId && mb.Month == month && mb.Year == year);

            if (existingBudget != null)
            {
                existingBudget.PlannedAmount = amount;
                await _context.SaveChangesAsync();
                return existingBudget;
            }
            else
            {
                var newBudget = new MonthlyBudget
                {
                    CategoryId = categoryId,
                    Month = month,
                    Year = year,
                    PlannedAmount = amount
                };

                _context.MonthlyBudgets.Add(newBudget);
                await _context.SaveChangesAsync();
                return newBudget;
            }
        }

        public List<MonthlyBudget> GetMonthlyBudgets(int month, int year)
        {
            var budgets = _context.MonthlyBudgets
                .Include(mb => mb.Category)
                .Where(mb => mb.Month == month && mb.Year == year)
                .ToList();

            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            foreach (var budget in budgets)
            {
                budget.SpentAmount = _context.Transactions
                    .Where(t => t.CategoryId == budget.CategoryId && 
                               t.Date >= startDate && 
                               t.Date <= endDate && 
                               !t.IsIncome)
                    .Sum(t => t.Amount);
            }

            return budgets;
        }

        #endregion

        #region Settings Management

        public AppSettings GetAppSettings()
        {
            return _context.AppSettings.FirstOrDefault() ?? new AppSettings();
        }

        public async Task<bool> UpdateAppSettingsAsync(AppSettings settings)
        {
            try
            {
                var existingSettings = _context.AppSettings.FirstOrDefault();
                if (existingSettings != null)
                {
                    _context.Entry(existingSettings).CurrentValues.SetValues(settings);
                }
                else
                {
                    _context.AppSettings.Add(settings);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}