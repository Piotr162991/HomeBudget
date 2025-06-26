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

    }
}