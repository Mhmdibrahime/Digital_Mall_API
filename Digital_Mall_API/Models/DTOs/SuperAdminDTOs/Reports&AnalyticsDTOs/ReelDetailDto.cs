namespace Digital_Mall_API.Models.DTOs.SuperAdminDTOs.Reports_AnalyticsDTOs
{
    public class ReelDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime PublishDate { get; set; }
        public int Likes { get; set; }
        public int Shares { get; set; }
        public int DurationInSeconds { get; set; }
        public string ThumbnailUrl { get; set; }
        public string VideoUrl { get; set; }
    }
}
