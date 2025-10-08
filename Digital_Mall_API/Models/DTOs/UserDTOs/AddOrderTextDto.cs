namespace Digital_Mall_API.Models.DTOs.UserDTOs
{
    public class AddOrderTextDto
    {
        public string Text { get; set; }
        public string FontFamily { get; set; } = "Arial";
        public string FontColor { get; set; } = "#000000";
        public string FontSize { get; set; } = "M";
        public string FontStyle { get; set; } = "Normal";
    }
}
