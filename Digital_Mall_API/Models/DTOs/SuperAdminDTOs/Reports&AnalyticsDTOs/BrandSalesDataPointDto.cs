namespace Digital_Mall_API.Models.DTOs.SuperAdminDTOs.Reports_AnalyticsDTOs
{
    public class BrandSalesDataPointDto
    {
        public string BrandId { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public decimal Sales { get; set; }
        public int OrderCount { get; set; }
    }
}
