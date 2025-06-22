using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HouseBug.Models
{
    public class Transaction
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        
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
        
        // Właściwości pomocnicze (nie mapowane do bazy)
        [NotMapped]
        public string TransactionType => IsIncome ? "Przychód" : "Wydatek";
        
        [NotMapped]
        public string FormattedAmount => IsIncome ? $"+{Amount:C}" : $"-{Amount:C}";
    }
}