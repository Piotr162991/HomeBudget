using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HouseBug.Models
{
    public class MonthlyBudget
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int CategoryId { get; set; }
        
        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PlannedAmount { get; set; }
        
        [Required]
        public int Month { get; set; }
        
        [Required]
        public int Year { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // Właściwości pomocnicze
        [NotMapped]
        public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM yyyy");
        
        [NotMapped]
        public decimal SpentAmount { get; set; } // Będzie obliczane dynamicznie
        
        [NotMapped]
        public decimal RemainingAmount => PlannedAmount - SpentAmount;
        
        [NotMapped]
        public double PercentageUsed => PlannedAmount > 0 ? (double)(SpentAmount / PlannedAmount) * 100 : 0;
    }
}