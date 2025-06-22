using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HouseBug.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Name { get; set; }
        
        [MaxLength(200)]
        public string Description { get; set; }
        
        [MaxLength(20)]
        public string Color { get; set; } = "#3498db";
        
        [MaxLength(50)]
        public string Icon { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        
        // Metoda pomocnicza
        public override string ToString()
        {
            return Name;
        }
    }
}