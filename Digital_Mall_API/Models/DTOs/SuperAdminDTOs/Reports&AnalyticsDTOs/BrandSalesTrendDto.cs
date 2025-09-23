namespace Digital_Mall_API.Models.DTOs.SuperAdminDTOs.Reports_AnalyticsDTOs
{
    public class BrandSalesTrendDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string BrandId { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public decimal Sales { get; set; }
        public int Orders { get; set; }
        public DateTime Period { get; set; }
    }
}
