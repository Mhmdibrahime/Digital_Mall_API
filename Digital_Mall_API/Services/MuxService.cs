using System.Text;
using System.Text.Json;

namespace Digital_Mall_API.Services
{
    public class MuxService : IMuxService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MuxService> _logger;

        public MuxService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<MuxService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<MuxUploadResponse> CreateDirectUploadAsync(int reelId, string corsOrigin)
        {
            var client = _httpClientFactory.CreateClient("MuxClient");

            var tokenId = "628e149a-a41b-49f4-8ac0-7a727b2b773f";
            var tokenSecret = "8BLArJ0SevMaIY3OXSCzro/F2R9t/nPE9qTrCJ6qb39uhsqIKPH9ZpbUxxg5g9Nf813kCG3G1tp";

            var authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{tokenId}:{tokenSecret}"));
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authValue);

            var request = new MuxCreateUploadRequest
            {
                NewAssetSettings = new MuxNewAssetSettings
                {
                    Passthrough = reelId.ToString(),
                    PlaybackPolicy = new[] { "public" },
                   },
                CorsOrigin = corsOrigin
            };

            try
            {
                var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

                var requestWithoutQuality = new
                {
                    new_asset_settings = new
                    {
                        passthrough = reelId.ToString(),
                        playback_policy = new[] { "public" }
                    },
                    cors_origin = corsOrigin
                };

                var jsonContent = JsonSerializer.Serialize(requestWithoutQuality, jsonOptions);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("/video/v1/uploads", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Mux API error: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);
                    throw new Exception($"Mux API returned {response.StatusCode}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<MuxUploadResponse>(responseContent, jsonOptions);

                _logger.LogInformation("Created Mux upload for reel {ReelId}, Mux upload ID: {MuxUploadId}",
                    reelId, result.Data.Id);

                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error calling Mux API for reel {ReelId}", reelId);
                throw new Exception("Network error communicating with Mux service", ex);
            }
        }
    }
}