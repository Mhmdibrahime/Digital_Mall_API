using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.DTOs.BrandAdminDTOs.ProductDiscountAndPromoCodeDTOs
{
    public class CreateDiscountDto
        {
            [Required]
            [Range(0, 100)]
            public decimal DiscountValue { get; set; }

            [StringLength(20)]    
            public string Status { get; set; } = "Active";

        public List<int>? ProductIds { get; set; }
    }
}
