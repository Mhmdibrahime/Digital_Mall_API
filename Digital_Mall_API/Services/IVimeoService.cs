namespace Digital_Mall_API.Services
{
    public interface IVimeoService
    {
        Task<VimeoUploadResponse> CreateVideoUploadAsync(int reelId);
        Task<VimeoVideoResponse> GetVideoAsync(string videoId);
        Task<VimeoVideoResponse> GetVideoByUriAsync(string videoUri);
        Task<bool> DeleteVideoAsync(string videoId);
    }
}