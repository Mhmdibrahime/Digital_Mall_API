namespace Digital_Mall_API.Models.DTOs.BrandAdminDTOs.WidgetsDTOs
{
    public class TotalOrdersDto
    {
        public int Count { get; set; }
        public decimal PercentageChange { get; set; }
        public string Trend { get; set; } = "increase";
    }
}
