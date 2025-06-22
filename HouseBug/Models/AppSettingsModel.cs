using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HouseBug.Models
{
    public class AppSettings
    {
        [Key]
        public int Id { get; set; }
        
        [MaxLength(10)]
        public string DefaultCurrency { get; set; } = "PLN";
        
        [MaxLength(20)]
        public string DateFormat { get; set; } = "dd.MM.yyyy";
        
        public bool ShowNotifications { get; set; } = true;
        
        public bool AutoBackup { get; set; } = false;
        
        public int BackupFrequencyDays { get; set; } = 7;
        
        [MaxLength(500)]
        public string BackupPath { get; set; }
        
        public bool DarkMode { get; set; } = false;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyBudgetLimit { get; set; } = 0;
        
        public bool EnableBudgetWarnings { get; set; } = true;
        
        public int BudgetWarningPercentage { get; set; } = 80;
    }
}