using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.DTOs.BrandAdminDTOs.ProductDiscountAndPromoCodeDTOs
{
    public class UpdateDiscountDto
        {
          

            [Range(0, 100)]
            public decimal? DiscountValue { get; set; }

            public string? Status { get; set; }

            public List<int>? ProductIds { get; set; }
        }
    
}
