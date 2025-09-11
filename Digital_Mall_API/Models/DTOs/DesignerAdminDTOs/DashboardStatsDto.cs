namespace Digital_Mall_API.Models.DTOs.DesignerAdminDTOs
{
    public class DashboardStatsDto
    {
        public int TotalRequests { get; set; }
        public double TotalRequestsChange { get; set; }

        public int PendingRequests { get; set; }
        public double PendingRequestsChange { get; set; }

        public int CompletedDesigns { get; set; }
        public double CompletedDesignsChange { get; set; }

        public decimal Earnings { get; set; }
        public double EarningsChange { get; set; }
    }


}
