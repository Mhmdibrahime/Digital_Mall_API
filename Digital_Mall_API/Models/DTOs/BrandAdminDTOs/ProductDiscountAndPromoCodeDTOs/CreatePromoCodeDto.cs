using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.DTOs.BrandAdminDTOs.ProductDiscountAndPromoCodeDTOs
{
    public class CreatePromoCodeDto
    {
        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0, 100)]
        public decimal DiscountValue { get; set; }

        [Required]
        [RegularExpression("^(Percentage|Fixed)$")]
        public string DiscountType { get; set; } = "Percentage";

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }
    }
}
