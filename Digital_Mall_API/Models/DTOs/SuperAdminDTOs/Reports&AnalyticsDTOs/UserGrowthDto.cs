namespace Digital_Mall_API.Models.DTOs.SuperAdminDTOs.Reports_AnalyticsDTOs
{
    public class UserGrowthMonthlyDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public int Customers { get; set; }
        public int Brands { get; set; }
        public int Models { get; set; }
        public int TotalUsers => Customers + Brands + Models;
    }
}
