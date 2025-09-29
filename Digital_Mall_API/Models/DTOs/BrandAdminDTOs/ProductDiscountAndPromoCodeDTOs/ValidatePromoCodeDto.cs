using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.DTOs.BrandAdminDTOs.ProductDiscountAndPromoCodeDTOs
{
    public class ValidatePromoCodeDto
    {
        [Required]
        public string Code { get; set; } = string.Empty;

        [Required]
        public string CustomerId { get; set; } = string.Empty;
    }
}
