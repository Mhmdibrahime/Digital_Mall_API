namespace Digital_Mall_API.Models.DTOs.UserDTOs.ModelProfileDTOs
{
    public class ModelReelDto
    {
        public int Id { get; set; }
        public string VideoUrl { get; set; }
        public string ThumbnailUrl { get; set; }
        public string Caption { get; set; }
        public int LikesCount { get; set; }
        public int SharesCount { get; set; }
        public DateTime PostedDate { get; set; }
        public int DurationInSeconds { get; set; }
        public List<ReelProductDto> Products { get; set; }
    }
}
