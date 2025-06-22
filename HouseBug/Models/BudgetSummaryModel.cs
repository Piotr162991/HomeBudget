using System;
using System.Collections.Generic;

namespace HouseBug.Models
{
    public class BudgetSummary
    {
        public DateTime Period { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal Balance => TotalIncome - TotalExpenses;
        public List<CategorySummary> CategorySummaries { get; set; } = new List<CategorySummary>();
        public int TransactionCount { get; set; }
        
        public string FormattedPeriod => Period.ToString("MMMM yyyy");
        public string BalanceStatus => Balance >= 0 ? "Nadwyżka" : "Deficyt";
    }
    
    public class CategorySummary
    {
        public string CategoryName { get; set; }
        public decimal Amount { get; set; }
        public int TransactionCount { get; set; }
        public double Percentage { get; set; }
        public string Color { get; set; }
    }
}