namespace Digital_Mall_API.Models.DTOs.ModelAdminDTOs.ReelUploadDTOs
{
    public class ReelUploadStatusResponse
    {
        public int ReelId { get; set; }
        public string UploadStatus { get; set; }
        public string? MuxAssetId { get; set; }
        public string? MuxPlaybackId { get; set; }
        public string VideoUrl { get; set; }
        public string ThumbnailUrl { get; set; }
        public string? Error { get; set; }
    }
}
