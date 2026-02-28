using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Digital_Mall_API.Models.Entities.Orders___Shopping
{
    public class ShippingGovernorate
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string EnglishName { get; set; }

        [Required]
        [StringLength(100)]
        public string ArabicName { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}