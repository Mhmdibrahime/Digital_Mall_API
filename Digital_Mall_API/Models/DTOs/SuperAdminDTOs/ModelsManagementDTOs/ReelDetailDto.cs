namespace Digital_Mall_API.Models.DTOs.SuperAdminDTOs.ModelsManagementDTOs
{
    public class ReelDetailDto
    {
        public int Id { get; set; }
        public string VideoUrl { get; set; }
        public string ThumbnailUrl { get; set; }
        public string Caption { get; set; }
        public DateTime PostedDate { get; set; }
        public int DurationInSeconds { get; set; }
        public int LikesCount { get; set; }
        public int LinkedProductsCount { get; set; }
    }
}
