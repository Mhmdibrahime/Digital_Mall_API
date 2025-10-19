namespace Digital_Mall_API.Models.DTOs.DesignerAdminDTOs
{
    public class MonthlyRequestDto
    {
        public string MonthName { get; set; } = string.Empty;
        public int Done { get; set; }
        public int Pending { get; set; }
        public int Rejected { get; set; }
    }


}
