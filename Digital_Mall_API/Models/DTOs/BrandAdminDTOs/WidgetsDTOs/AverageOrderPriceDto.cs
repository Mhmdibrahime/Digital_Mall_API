namespace Digital_Mall_API.Models.DTOs.BrandAdminDTOs.WidgetsDTOs
{
    public class AverageOrderPriceDto
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "LE";
        public string Description { get; set; } = "per order";
    }
}
