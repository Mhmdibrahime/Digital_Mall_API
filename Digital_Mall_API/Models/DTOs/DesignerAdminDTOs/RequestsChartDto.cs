namespace Digital_Mall_API.Models.DTOs.DesignerAdminDTOs
{
    public class RequestsChartDto
    {
        public int Year { get; set; }
        public List<MonthlyRequestDto> MonthlyRequests { get; set; } = new List<MonthlyRequestDto>();
    }


}
