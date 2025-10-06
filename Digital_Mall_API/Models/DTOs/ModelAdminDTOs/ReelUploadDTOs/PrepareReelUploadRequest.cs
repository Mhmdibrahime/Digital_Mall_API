namespace Digital_Mall_API.Models.DTOs.ModelAdminDTOs.ReelUploadDTOs
{
    public class PrepareReelUploadRequest
    {
        public string? Caption { get; set; }
        public int DurationInSeconds { get; set; }
        public List<int>? LinkedProductIds { get; set; }
    }
}
