using System.Text.Json.Serialization;

namespace Digital_Mall_API.Services
{
    public interface IMuxService
    {
        Task<MuxUploadResponse> CreateDirectUploadAsync(int reelId, string corsOrigin);
    }

    // DTOs for Mux API
    public class MuxCreateUploadRequest
    {
        [JsonPropertyName("new_asset_settings")]
        public MuxNewAssetSettings NewAssetSettings { get; set; }

        [JsonPropertyName("cors_origin")]
        public string CorsOrigin { get; set; }
    }

    public class MuxNewAssetSettings
    {
        [JsonPropertyName("passthrough")]
        public string Passthrough { get; set; }

        [JsonPropertyName("playback_policy")]
        public string[] PlaybackPolicy { get; set; }

    }

    public class MuxUploadResponse
    {
        [JsonPropertyName("data")]
        public MuxUploadData Data { get; set; }
    }

    public class MuxUploadData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }
    }
}