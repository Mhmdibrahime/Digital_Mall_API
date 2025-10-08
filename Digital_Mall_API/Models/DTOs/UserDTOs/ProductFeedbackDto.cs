namespace Digital_Mall_API.Models.DTOs.UserDTOs
{
    public class ProductFeedbackDto
    {
        public int Id { get; set; }
        public string? UserName { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
