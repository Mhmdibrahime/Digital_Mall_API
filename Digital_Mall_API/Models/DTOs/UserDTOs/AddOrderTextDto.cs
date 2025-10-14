namespace Digital_Mall_API.Models.DTOs.UserDTOs
{
    public class AddOrderTextDto
    {
        public string text { get; set; } = "None";
        public string fontFamily { get; set; } = "Arial";
        public string fontColor { get; set; } = "#000000";
        public int fontSize { get; set; } = 0;
        public string fontStyle { get; set; } = "Normal";
    }
}
