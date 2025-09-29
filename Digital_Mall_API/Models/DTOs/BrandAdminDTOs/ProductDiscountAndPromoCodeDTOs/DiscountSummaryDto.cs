namespace Digital_Mall_API.Models.DTOs.BrandAdminDTOs.ProductDiscountAndPromoCodeDTOs
{
    public class DiscountSummaryDto
        {
            public int TotalDiscounts { get; set; }
            public int ActiveDiscounts { get; set; }
            public int InactiveDiscounts { get; set; }
            public int PromoCodes { get; set; }
        }
    
}
