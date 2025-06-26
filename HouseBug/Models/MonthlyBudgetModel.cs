using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HouseBug.Models
{
    public class MonthlyBudget : INotifyPropertyChanged
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int CategoryId { get; set; }
        
        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; }
        
        private decimal _plannedAmount;
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PlannedAmount
        {
            get => _plannedAmount;
            set { if (_plannedAmount != value) { _plannedAmount = value; OnPropertyChanged(nameof(PlannedAmount)); OnPropertyChanged(nameof(RemainingAmount)); OnPropertyChanged(nameof(PercentageUsed)); } }
        }
        
        [Required]
        public int Month { get; set; }
        
        [Required]
        public int Year { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        [NotMapped]
        public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM yyyy", new System.Globalization.CultureInfo("pl-PL"));
        
        private decimal _spentAmount;
        [NotMapped]
        public decimal SpentAmount
        {
            get => _spentAmount;
            set { if (_spentAmount != value) { _spentAmount = value; OnPropertyChanged(nameof(SpentAmount)); OnPropertyChanged(nameof(RemainingAmount)); OnPropertyChanged(nameof(PercentageUsed)); } }
        }
        
        [NotMapped]
        public decimal RemainingAmount => PlannedAmount - SpentAmount;
        
        [NotMapped]
        public double PercentageUsed => PlannedAmount > 0 ? (double)(SpentAmount / PlannedAmount) * 100 : 0;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        
        public void ForceUpdateAmounts()
        {
            OnPropertyChanged(nameof(PlannedAmount));
            OnPropertyChanged(nameof(SpentAmount));
            OnPropertyChanged(nameof(RemainingAmount));
            OnPropertyChanged(nameof(PercentageUsed));
        }
    }
}