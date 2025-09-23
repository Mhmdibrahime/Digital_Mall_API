namespace Digital_Mall_API.Models.DTOs.SuperAdminDTOs.Reports_AnalyticsDTOs
{
    public class ModelReelsSummaryDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public DateTime JoinDate { get; set; }
        public int TotalReels { get; set; }
        public int TotalLikes { get; set; }
        public int TotalShares { get; set; }
        public double AverageLikes { get; set; }
        public double AverageShares { get; set; }
    }
}
