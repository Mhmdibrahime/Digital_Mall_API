using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.Entities.Logs;
using Digital_Mall_API.Models.Entities.Reels___Content;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Digital_Mall_API.Controllers.Reels
{
    [ApiController]
    [Route("api/webhooks/mux")]
    public class MuxWebhookController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<MuxWebhookController> _logger;

        public MuxWebhookController(AppDbContext context, ILogger<MuxWebhookController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private async Task LogToDatabase(string logLevel, string webhookType, int? reelId, string message, string details = null)
        {
            try
            {
                var log = new WebhookLog
                {
                    LogLevel = logLevel,
                    WebhookType = webhookType,
                    Message = message,
                    Details = details
                };

                _context.WebhookLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write webhook log to database");
            }
        }

        [HttpPost]
        public async Task<IActionResult> HandleMuxWebhook()
        {
            string requestBody = null;
            int? reelId = null;

            try
            {
                using var reader = new StreamReader(HttpContext.Request.Body);
                requestBody = await reader.ReadToEndAsync();

                await LogToDatabase("INFO", "REQUEST_RECEIVED", null, "Webhook endpoint hit", requestBody);
                await DebugRawJsonStructure(requestBody);

                // ✅ FIX: remove camelCase policy (Mux uses snake_case)
                var webhook = JsonSerializer.Deserialize<MuxWebhook>(requestBody);

                if (webhook == null)
                {
                    await LogToDatabase("ERROR", "INVALID_PAYLOAD", null, "Failed to deserialize webhook payload", requestBody);
                    return BadRequest("Invalid webhook payload");
                }

                reelId = ExtractReelIdFromWebhook(webhook, requestBody);

                if (!reelId.HasValue)
                {
                    await LogToDatabase("WARNING", webhook.Type, null, "Invalid or missing passthrough ID", $"Passthrough from data: {webhook.Data?.Passthrough}");
                    return BadRequest("Invalid passthrough ID");
                }

                await LogToDatabase("INFO", webhook.Type, reelId, $"Webhook received", $"Type: {webhook.Type}, Asset ID: {webhook.Data?.Id}");

                var reel = await _context.Reels.FindAsync(reelId);
                if (reel == null)
                {
                    await LogToDatabase("ERROR", webhook.Type, reelId, "Reel not found in database", $"Reel ID: {reelId} not found");
                    return NotFound($"Reel {reelId} not found");
                }

                switch (webhook.Type)
                {
                    case "video.upload.asset_created":
                        await ProcessAssetCreated(reel, webhook.Data);
                        break;

                    case "video.asset.created":
                        await ProcessAssetCreatedWebhook(reel, webhook.Data, requestBody);
                        break;

                    case "video.asset.ready":
                        await ProcessAssetReady(reel, webhook.Data, requestBody);
                        break;

                    case "video.asset.errored":
                        await ProcessAssetErrored(reel, webhook.Data);
                        break;

                    case "video.upload.cancelled":
                        await ProcessUploadCancelled(reel, webhook.Data);
                        break;

                    default:
                        await LogToDatabase("INFO", webhook.Type, reelId, "Unhandled webhook type", $"Webhook type: {webhook.Type} was received but not processed");
                        break;
                }

                await _context.SaveChangesAsync();
                await LogToDatabase("INFO", webhook.Type, reelId, "Webhook processed successfully", "All changes saved to database");

                return Ok();
            }
            catch (Exception ex)
            {
                await LogToDatabase("ERROR", "EXCEPTION", reelId, "Critical error processing webhook", $"Exception: {ex.Message}, StackTrace: {ex.StackTrace}, RequestBody: {requestBody}");
                _logger.LogError(ex, "💀 CRITICAL ERROR processing Mux webhook");
                return StatusCode(500, "Internal server error");
            }
        }

        private async Task DebugRawJsonStructure(string requestBody)
        {
            try
            {
                using var doc = JsonDocument.Parse(requestBody);
                var root = doc.RootElement;

                var debugInfo = new StringBuilder();
                debugInfo.AppendLine("=== RAW JSON DEBUG ===");

                if (root.TryGetProperty("type", out var type))
                    debugInfo.AppendLine($"Type: {type}");

                if (root.TryGetProperty("data", out var data))
                {
                    debugInfo.AppendLine("Data properties:");
                    foreach (var prop in data.EnumerateObject())
                    {
                        debugInfo.AppendLine($"  {prop.Name}: {prop.Value.ValueKind}");

                        if (prop.Name == "playback_ids" && prop.Value.ValueKind == JsonValueKind.Array)
                        {
                            debugInfo.AppendLine($"  playback_ids count: {prop.Value.GetArrayLength()}");
                            foreach (var item in prop.Value.EnumerateArray())
                            {
                                debugInfo.AppendLine($"    - {item}");
                            }
                        }
                    }
                }

                await LogToDatabase("DEBUG", "JSON_STRUCTURE", null, "Raw JSON structure analysis", debugInfo.ToString());
            }
            catch (Exception ex)
            {
                await LogToDatabase("ERROR", "JSON_DEBUG", null, "Failed to analyze JSON structure", ex.Message);
            }
        }

        private int? ExtractReelIdFromWebhook(MuxWebhook webhook, string requestBody)
        {
            if (!string.IsNullOrEmpty(webhook.Data?.Passthrough) && int.TryParse(webhook.Data.Passthrough, out int id1))
                return id1;

            try
            {
                using var doc = JsonDocument.Parse(requestBody);
                if (doc.RootElement.TryGetProperty("data", out var data) &&
                    data.TryGetProperty("new_asset_settings", out var newAssetSettings) &&
                    newAssetSettings.TryGetProperty("passthrough", out var passthrough) &&
                    passthrough.ValueKind == JsonValueKind.String &&
                    int.TryParse(passthrough.GetString(), out int id2))
                    return id2;
            }
            catch { }

            return null;
        }

        private async Task ProcessAssetCreated(Reel reel, MuxWebhookData data)
        {
            reel.MuxAssetId = data.Id;
            reel.UploadStatus = "processing";
            await LogToDatabase("INFO", "video.upload.asset_created", reel.Id, "Asset created", $"MuxAssetId: {data.Id}, Status: processing");
        }

        private async Task ProcessAssetCreatedWebhook(Reel reel, MuxWebhookData data, string rawRequestBody)
        {
            reel.MuxAssetId = data.Id;
            reel.UploadStatus = "processing";

            if (data.PlaybackIds != null && data.PlaybackIds.Any())
            {
                var playback = data.PlaybackIds.First();
                reel.MuxPlaybackId = playback.Id;
                reel.VideoUrl = $"https://stream.mux.com/{reel.MuxPlaybackId}.m3u8";
                reel.ThumbnailUrl = $"https://image.mux.com/{reel.MuxPlaybackId}/thumbnail.jpg";

                await LogToDatabase("INFO", "video.asset.created", reel.Id, "Asset created WITH playback IDs", $"PlaybackId: {reel.MuxPlaybackId}");
            }
            else
            {
                await LogToDatabase("WARNING", "video.asset.created", reel.Id, "Asset created but NO playback IDs found", "PlaybackIds array empty");
            }
        }

        private async Task ProcessAssetReady(Reel reel, MuxWebhookData data, string rawRequestBody)
        {
            try
            {
                // 🔹 Basic updates
                reel.MuxAssetId = data.Id;
                reel.UploadStatus = "ready";
                reel.DurationInSeconds = (int)Math.Ceiling(data.Duration ?? 0);

                // 🔹 Extract Playback ID (if exists)
                string? playbackId = data.PlaybackIds?.FirstOrDefault()?.Id;

                if (!string.IsNullOrWhiteSpace(playbackId))
                {
                    reel.MuxPlaybackId = playbackId;
                    reel.VideoUrl = $"https://stream.mux.com/{playbackId}.m3u8";
                    reel.ThumbnailUrl = $"https://image.mux.com/{playbackId}/thumbnail.jpg";

                    await LogToDatabase(
                        "INFO",
                        "video.asset.ready",
                        reel.Id,
                        "Asset ready WITH URLs",
                        $"✅ FINAL - PlaybackId: {playbackId}\n✅ FINAL - Video URL: {reel.VideoUrl}"
                    );
                }
                else
                {
                    // 🔸 If Mux didn’t include a playback ID, fallback to API
                    await LogToDatabase(
                        "WARNING",
                        "video.asset.ready",
                        reel.Id,
                        "Asset ready but NO playback ID",
                        "Attempting to retrieve playback ID from Mux API..."
                    );

                    await TryGetPlaybackIdFromMuxApi(reel, data.Id);
                }

                // 🔹 Optional: log duration + resolution if available
                string extraInfo = $"Duration: {reel.DurationInSeconds}s ";
                await LogToDatabase("DEBUG", "video.asset.ready", reel.Id, "Metadata", extraInfo);

                // 🔹 Save all changes
                await _context.SaveChangesAsync();

                await LogToDatabase("INFO", "video.asset.ready", reel.Id, "Webhook processed successfully", "All changes saved to database");
            }
            catch (Exception ex)
            {
                await LogToDatabase("ERROR", "video.asset.ready", reel?.Id, "Error processing video.asset.ready", ex.ToString());
                throw;
            }
        }


        private async Task ProcessAssetErrored(Reel reel, MuxWebhookData data)
        {
            reel.UploadStatus = "failed";
            reel.UploadError = data.Errors?.Message ?? "Unknown error";
            await LogToDatabase("ERROR", "video.asset.errored", reel.Id, "Asset processing failed", $"Error: {reel.UploadError}");
        }

        private async Task ProcessUploadCancelled(Reel reel, MuxWebhookData data)
        {
            reel.UploadStatus = "cancelled";
            await LogToDatabase("INFO", "video.upload.cancelled", reel.Id, "Upload cancelled", $"MuxAssetId: {data.Id}");
        }

        private async Task TryGetPlaybackIdFromMuxApi(Reel reel, string muxAssetId)
        {
            try
            {
                await LogToDatabase("INFO", "MUX_API_FALLBACK", reel.Id, "Attempting to fetch playback ID from Mux API", $"Asset ID: {muxAssetId}");

                var muxTokenId = "628e149a-a41b-49f4-8ac0-7a727b2b773f";
                var muxTokenSecret = "8BLArJ0SevMaIY3OXSCzro/F2R9t/nPE9qTrCJ6qb39uhsqIKPH9ZpbUxxg5g9Nf813kCG3G1tp";
                var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{muxTokenId}:{muxTokenSecret}"));

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                var response = await httpClient.GetAsync($"https://api.mux.com/video/v1/assets/{muxAssetId}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    await LogToDatabase("DEBUG", "MUX_API_RESPONSE", reel.Id, "Full Mux API response", content);

                    var assetData = JsonSerializer.Deserialize<MuxAssetResponse>(
                        content,
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                    );

                    // ✅ NEW: Update status if Mux reports the asset as ready
                    if (assetData?.Data?.Status == "ready")
                    {
                        reel.UploadStatus = "ready";
                        await LogToDatabase("INFO", "MUX_API_FALLBACK", reel.Id, "Mux asset is ready", $"Status: {assetData.Data.Status}");
                    }

                    if (assetData?.Data?.PlaybackIds != null && assetData.Data.PlaybackIds.Any())
                    {
                        reel.MuxPlaybackId = assetData.Data.PlaybackIds.First().Id;
                        reel.VideoUrl = $"https://stream.mux.com/{reel.MuxPlaybackId}.m3u8";
                        reel.ThumbnailUrl = $"https://image.mux.com/{reel.MuxPlaybackId}/thumbnail.jpg";

                        await _context.SaveChangesAsync();
                        await LogToDatabase("INFO", "MUX_API_FALLBACK", reel.Id, "Successfully retrieved playback ID from Mux API", $"PlaybackId: {reel.MuxPlaybackId}");
                    }
                    else
                    {
                        await LogToDatabase("WARNING", "MUX_API_FALLBACK", reel.Id, "Mux API returned no playback IDs", $"Asset Status: {assetData?.Data?.Status}");
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await LogToDatabase("ERROR", "MUX_API_FALLBACK", reel.Id, "Mux API request failed", $"Status: {response.StatusCode}, Error: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                await LogToDatabase("ERROR", "MUX_API_FALLBACK", reel.Id, "Failed to fetch playback ID from Mux API", $"Exception: {ex.Message}");
            }
        }

        // ✅ Correct JSON model mapping for snake_case
        public class MuxWebhook
        {
            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("data")]
            public MuxWebhookData Data { get; set; }
        }

        public class MuxWebhookData
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("passthrough")]
            public string Passthrough { get; set; }

            [JsonPropertyName("playback_ids")]
            public List<MuxPlaybackId> PlaybackIds { get; set; }

            [JsonPropertyName("duration")]
            public double? Duration { get; set; }

            [JsonPropertyName("errors")]
            public MuxError Errors { get; set; }
        }

        public class MuxPlaybackId
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("policy")]
            public string Policy { get; set; }
        }

        public class MuxError
        {
            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("message")]
            public string Message { get; set; }
        }

        public class MuxAssetResponse
        {
            [JsonPropertyName("data")]
            public MuxAssetData Data { get; set; }
        }

        public class MuxAssetData
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("status")]
            public string Status { get; set; }

            [JsonPropertyName("duration")]
            public double? Duration { get; set; }

            [JsonPropertyName("playback_ids")]
            public List<MuxPlaybackId> PlaybackIds { get; set; }

            [JsonPropertyName("passthrough")]
            public string Passthrough { get; set; }
        }
    }
}
