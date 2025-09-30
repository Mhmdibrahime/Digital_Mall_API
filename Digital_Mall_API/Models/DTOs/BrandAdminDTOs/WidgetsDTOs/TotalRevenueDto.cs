namespace Digital_Mall_API.Models.DTOs.BrandAdminDTOs.WidgetsDTOs
{
    public class TotalRevenueDto
    {
        public decimal Amount { get; set; }
        public decimal PercentageChange { get; set; }
        public string Trend { get; set; } = "increase";
        public string Currency { get; set; } = "LE";
    }
}
