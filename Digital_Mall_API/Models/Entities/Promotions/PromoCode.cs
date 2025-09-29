using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Digital_Mall_API.Models.Entities.Promotions
{
    public class PromoCode
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; } 

        [Required]
        [StringLength(200)]
        public string Name { get; set; } 

       

        [Required]
        [Range(0, 100)]
        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountValue { get; set; }

       

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Active"; 

        public bool IsSingleUse { get; set; } = true;
        public int CurrentUsageCount { get; set; } = 0;

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual List<PromoCodeUsage> Usages { get; set; } = new List<PromoCodeUsage>();
    }
}