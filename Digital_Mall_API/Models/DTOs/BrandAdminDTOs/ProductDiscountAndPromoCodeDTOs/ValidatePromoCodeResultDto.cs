namespace Digital_Mall_API.Models.DTOs.BrandAdminDTOs.ProductDiscountAndPromoCodeDTOs
{
    public class ValidatePromoCodeResultDto
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal DiscountValue { get; set; }
        public string DiscountType { get; set; } = string.Empty;
        public decimal DiscountAmount { get; set; }
        public string PromoCodeName { get; set; } = string.Empty;
    }
}
