using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Digital_Mall_API.Services
{

    public class VimeoService : IVimeoService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<VimeoService> _logger;

        public VimeoService(HttpClient httpClient, IConfiguration configuration, ILogger<VimeoService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            // Configure Vimeo API
            var accessToken = _configuration["Vimeo:AccessToken"];
            if (!string.IsNullOrEmpty(accessToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.vimeo.*+json;version=3.4");
            }
            _httpClient.BaseAddress = new Uri("https://api.vimeo.com/");
        }

        public async Task<VimeoUploadResponse> CreateVideoUploadAsync(int reelId)
        {
            try
            {
                var embedDomain = _configuration["Vimeo:EmbedDomain"] ?? "localhost";

                var request = new
                {
                    upload = new
                    {
                        approach = "post"
                    },
                    name = $"Reel_{reelId}_{DateTime.UtcNow:yyyyMMddHHmmss}",
                    description = $"Reel content - ReelId:{reelId}", // Include ReelId in description
                    privacy = new
                    {
                        view = "disable", // Hide from Vimeo.com
                        embed = "public",
                        embed_domains = embedDomain
                    },
                    embed = new
                    {
                        buttons = new
                        {
                            like = false,
                            watchlater = false,
                            share = false
                        },
                        logos = new
                        {
                            vimeo = false
                        },
                        title = new
                        {
                            name = "show",
                            owner = "hide",
                            portrait = "hide"
                        }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(request);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogInformation("Creating Vimeo upload for reel {ReelId}", reelId);

                var response = await _httpClient.PostAsync("me/videos", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Vimeo API error: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);
                    throw new Exception($"Vimeo API returned {response.StatusCode}: {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<VimeoUploadResponse>(responseContent);

                if (result == null)
                    throw new Exception("Failed to deserialize Vimeo response");

                _logger.LogInformation("Created Vimeo upload for reel {ReelId}, Vimeo video ID: {VideoId}",
                    reelId, result.VideoId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Vimeo upload for reel {ReelId}", reelId);
                throw;
            }
        }

        public async Task<VimeoVideoResponse> GetVideoAsync(string videoId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"videos/{videoId}?fields=uri,name,description,status,duration,link,player_embed_url,embed.html,files,pictures");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Vimeo API returned {response.StatusCode}: {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<VimeoVideoResponse>(responseContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Vimeo video {VideoId}", videoId);
                throw;
            }
        }

        public async Task<VimeoVideoResponse> GetVideoByUriAsync(string videoUri)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{videoUri}?fields=uri,name,description,status,duration,link,player_embed_url,embed.html,files,pictures");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Vimeo API returned {response.StatusCode}: {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<VimeoVideoResponse>(responseContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Vimeo video by URI {VideoUri}", videoUri);
                throw;
            }
        }

        public async Task<bool> DeleteVideoAsync(string videoId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"videos/{videoId}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to delete Vimeo video {VideoId}: {ErrorContent}", videoId, errorContent);
                    return false;
                }

                _logger.LogInformation("Deleted Vimeo video {VideoId}", videoId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Vimeo video {VideoId}", videoId);
                return false;
            }
        }
    }

    public class VimeoUploadResponse
    {
        [JsonPropertyName("uri")]
        public string Uri { get; set; }

        [JsonPropertyName("link")]
        public string Link { get; set; }

        [JsonPropertyName("upload")]
        public VimeoUpload Upload { get; set; }

        [JsonIgnore]
        public string VideoId => Uri?.Split('/').LastOrDefault();

        [JsonIgnore]
        public string UploadUrl => Upload?.UploadLink;
    }

    public class VimeoUpload
    {
        [JsonPropertyName("upload_link")]
        public string UploadLink { get; set; }

        [JsonPropertyName("form")]
        public string Form { get; set; }
    }

    public class VimeoVideoResponse
    {
        [JsonPropertyName("uri")]
        public string Uri { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("duration")]
        public decimal Duration { get; set; }

        [JsonPropertyName("link")]
        public string Link { get; set; }

        [JsonPropertyName("player_embed_url")]
        public string PlayerEmbedUrl { get; set; }

        [JsonPropertyName("embed")]
        public VimeoEmbed Embed { get; set; }

        [JsonPropertyName("files")]
        public List<VimeoFile> Files { get; set; }

        [JsonPropertyName("pictures")]
        public VimeoPictures Pictures { get; set; }

        [JsonIgnore]
        public string VideoId => Uri?.Split('/').LastOrDefault();
    }

    public class VimeoEmbed
    {
        [JsonPropertyName("html")]
        public string Html { get; set; }
    }

    public class VimeoFile
    {
        [JsonPropertyName("quality")]
        public string Quality { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("link")]
        public string Link { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }
    }

    public class VimeoPictures
    {
        [JsonPropertyName("sizes")]
        public List<VimeoPictureSize> Sizes { get; set; }
    }

    public class VimeoPictureSize
    {
        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("link")]
        public string Link { get; set; }
    }
}