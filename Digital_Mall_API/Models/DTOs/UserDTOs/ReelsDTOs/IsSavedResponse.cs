namespace Digital_Mall_API.Models.DTOs.UserDTOs.ReelsDTOs
{
    public class IsSavedResponse
    {
        public int ReelId { get; set; }
        public bool IsSaved { get; set; }
        public DateTime? SavedAt { get; set; }
    }
}
