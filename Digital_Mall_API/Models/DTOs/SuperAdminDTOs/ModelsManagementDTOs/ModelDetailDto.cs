namespace Digital_Mall_API.Models.DTOs.SuperAdminDTOs.ModelsManagementDTOs
{
    public class ModelDetailDto
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Location { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public int? Age { get; set; }
        public string Bio { get; set; }
        public string Status { get; set; }
        public decimal CommissionRate { get; set; }
        public string EvidenceOfProofUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ReelsCount { get; set; }
        public int TotalLikes { get; set; }
        public double AverageLikesPerReel { get; set; }
        public decimal TotalEarnings { get; set; }
        public decimal PendingEarnings { get; set; }
        public decimal PerformanceScore { get; set; }
        public DateTime? LastReelDate { get; set; }
        public DateTime? LastPayoutDate { get; set; }
        public List<ReelInfoDto> Reels { get; set; }
    }
}
