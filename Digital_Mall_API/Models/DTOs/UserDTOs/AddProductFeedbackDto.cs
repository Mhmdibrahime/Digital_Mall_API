namespace Digital_Mall_API.Models.DTOs.UserDTOs
{
    public class AddProductFeedbackDto
    {
        public int ProductId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }
}
