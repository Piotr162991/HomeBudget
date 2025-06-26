using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HouseBug.Models
{
    public class Transaction : INotifyPropertyChanged
    {
        [Key]
        public int Id { get; set; }
        
        private decimal _amount;
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount
        {
            get => _amount;
            set { if (_amount != value) { _amount = value; OnPropertyChanged(nameof(Amount)); } }
        }
        
        [Required]
        public DateTime Date { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Description { get; set; }
        
        [Required]
        public int CategoryId { get; set; }
        
        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; }
        
        public bool IsIncome { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }
        
        [NotMapped]
        public string TransactionType => IsIncome ? "Przychód" : "Wydatek";
        
        [NotMapped]
        public string FormattedAmount => IsIncome ? $"+{Amount:C}" : $"-{Amount:C}";
        
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        
        public void ForceUpdateAmounts()
        {
            OnPropertyChanged(nameof(Amount));
            OnPropertyChanged(nameof(FormattedAmount));
        }
    }
}