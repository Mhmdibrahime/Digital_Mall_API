namespace Digital_Mall_API.Models.DTOs.UserDTOs.ReelsDTOs
{
    public class SaveReelResponse
    {
        public int ReelId { get; set; }
        public bool IsSaved { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
