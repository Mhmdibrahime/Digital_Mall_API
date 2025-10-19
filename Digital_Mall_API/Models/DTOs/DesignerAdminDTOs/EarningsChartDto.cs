namespace Digital_Mall_API.Models.DTOs.DesignerAdminDTOs
{
    public class EarningsChartDto
    {
        public int Year { get; set; }
        public List<MonthlyEarningDto> MonthlyEarnings { get; set; } = new List<MonthlyEarningDto>();
    }


}
