namespace Digital_Mall_API.Models.DTOs.BrandAdminDTOs.WidgetsDTOs
{
    public class TotalProductsDto
    {
        public int Count { get; set; }
        public decimal PercentageChange { get; set; }
        public string Trend { get; set; } = "increase"; 
    }
}
