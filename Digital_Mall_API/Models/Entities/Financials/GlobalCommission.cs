using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Digital_Mall_API.Models.Entities.Financials
{
    public class GlobalCommission
    {
        [Key]
        public int Id { get; set; } = 1; 

        [Required]
        [Range(0, 100)]
        [Column(TypeName = "decimal(5,2)")]
        public decimal CommissionRate { get; set; } = 10.0m;

        
    }
}
