namespace Digital_Mall_API.Models.DTOs.BrandAdminDTOs.ProductDiscountAndPromoCodeDTOs
{
    public class PromoCodeSummaryDto
    {
        public int TotalPromoCodes { get; set; }
        public int ActivePromoCodes { get; set; }
        public int UsedPromoCodes { get; set; }
        public int ExpiredPromoCodes { get; set; }
    }
}
