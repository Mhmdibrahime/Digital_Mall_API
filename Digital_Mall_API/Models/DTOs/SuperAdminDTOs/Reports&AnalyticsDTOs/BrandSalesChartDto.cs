namespace Digital_Mall_API.Models.DTOs.SuperAdminDTOs.Reports_AnalyticsDTOs
{
    public class BrandSalesChartDto
    {
        public int Year { get; set; }
        public string ChartType { get; set; } = "bar";
        public List<BrandSalesDataPointDto> Data { get; set; } = new List<BrandSalesDataPointDto>();
        public string ChartTitle { get; set; } = string.Empty;
        public decimal TotalSales { get; set; }
        public int TotalOrders { get; set; }
    }
}
