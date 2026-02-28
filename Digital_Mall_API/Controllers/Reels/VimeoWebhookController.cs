using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.Entities.Logs;
using Digital_Mall_API.Models.Entities.Reels___Content;
using Digital_Mall_API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Digital_Mall_API.Controllers.Reels
{
    [ApiController]
    [Route("api/webhooks/vimeo")]
    public class VimeoWebhookController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<VimeoWebhookController> _logger;
        private readonly IVimeoService _vimeoService;

        public VimeoWebhookController(
            AppDbContext context,
            ILogger<VimeoWebhookController> logger,
            IVimeoService vimeoService)
        {
            _context = context;
            _logger = logger;
            _vimeoService = vimeoService;
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
        public async Task<IActionResult> HandleVimeoWebhook()
        {
            string requestBody = null;
            int? reelId = null;

            try
            {
                using var reader = new StreamReader(HttpContext.Request.Body);
                requestBody = await reader.ReadToEndAsync();

                await LogToDatabase("INFO", "REQUEST_RECEIVED", null, "Vimeo webhook endpoint hit", requestBody);

                var webhook = JsonSerializer.Deserialize<VimeoWebhook>(requestBody);

                if (webhook == null)
                {
                    await LogToDatabase("ERROR", "INVALID_PAYLOAD", null, "Failed to deserialize Vimeo webhook payload", requestBody);
                    return BadRequest("Invalid webhook payload");
                }

                // Extract reel ID from video metadata
                reelId = ExtractReelIdFromWebhook(webhook);

                if (!reelId.HasValue)
                {
                    // Try to extract from video description or name
                    reelId = await TryExtractReelIdFromVideo(webhook);

                    if (!reelId.HasValue)
                    {
                        await LogToDatabase("WARNING", webhook.Type, null, "Could not extract reel ID", $"Video ID: {webhook.VideoId}");
                        return BadRequest("Could not extract reel ID");
                    }
                }

                await LogToDatabase("INFO", webhook.Type, reelId, $"Vimeo webhook received", $"Type: {webhook.Type}, Video ID: {webhook.VideoId}");

                var reel = await _context.Reels.FindAsync(reelId);
                if (reel == null)
                {
                    await LogToDatabase("ERROR", webhook.Type, reelId, "Reel not found in database", $"Reel ID: {reelId} not found");
                    return NotFound($"Reel {reelId} not found");
                }

                switch (webhook.Type)
                {
                    case "video.created":
                        await ProcessVideoCreated(reel, webhook);
                        break;

                    case "video.transcode.complete":
                        await ProcessTranscodeComplete(reel, webhook);
                        break;

                    case "video.transcode.playable":
                        await ProcessTranscodePlayable(reel, webhook);
                        break;

                    case "video.transcode.fully.playable":
                        await ProcessTranscodeFullyPlayable(reel, webhook);
                        break;

                    case "video.upload.failed":
                        await ProcessUploadFailed(reel, webhook);
                        break;

                    case "video.deleted":
                        await ProcessVideoDeleted(reel, webhook);
                        break;

                    case "video.updated":
                        await ProcessVideoUpdated(reel, webhook);
                        break;

                    case "automatic-thumbnail-available":
                        await ProcessThumbnailAvailable(reel, webhook);
                        break;

                    default:
                        await LogToDatabase("INFO", webhook.Type, reelId, "Unhandled Vimeo webhook type", $"Webhook type: {webhook.Type} was received but not processed");
                        break;
                }

                await _context.SaveChangesAsync();
                await LogToDatabase("INFO", webhook.Type, reelId, "Vimeo webhook processed successfully", "All changes saved to database");

                return Ok();
            }
            catch (Exception ex)
            {
                await LogToDatabase("ERROR", "EXCEPTION", reelId, "Critical error processing Vimeo webhook", $"Exception: {ex.Message}, StackTrace: {ex.StackTrace}, RequestBody: {requestBody}");
                _logger.LogError(ex, "💀 CRITICAL ERROR processing Vimeo webhook");
                return StatusCode(500, "Internal server error");
            }
        }

        private int? ExtractReelIdFromWebhook(VimeoWebhook webhook)
        {
            // Vimeo webhooks might include metadata or custom fields
            // We need to store reelId in the video description or metadata during creation

            // Check if there's metadata in the webhook
            if (!string.IsNullOrEmpty(webhook.Metadata?.ReelId))
            {
                if (int.TryParse(webhook.Metadata.ReelId, out int id))
                    return id;
            }

            return null;
        }

        private async Task<int?> TryExtractReelIdFromVideo(VimeoWebhook webhook)
        {
            if (string.IsNullOrEmpty(webhook.VideoId))
                return null;

            try
            {
                // Fetch video details from Vimeo API
                var videoInfo = await _vimeoService.GetVideoAsync(webhook.VideoId);

                if (videoInfo == null)
                    return null;

                // Check video description for ReelId
                if (!string.IsNullOrEmpty(videoInfo.Description))
                {
                    var description = videoInfo.Description;

                    // Look for pattern: ReelId:123
                    if (description.Contains("ReelId:"))
                    {
                        var parts = description.Split("ReelId:");
                        if (parts.Length > 1 && int.TryParse(parts[1].Trim().Split(' ').First(), out int id))
                            return id;
                    }
                }

                // Check video name for pattern: Reel_123_...
                if (!string.IsNullOrEmpty(videoInfo.Name) && videoInfo.Name.StartsWith("Reel_"))
                {
                    var parts = videoInfo.Name.Split('_');
                    if (parts.Length > 1 && int.TryParse(parts[1], out int id))
                        return id;
                }

                // Check URI for numeric ID pattern
                if (!string.IsNullOrEmpty(videoInfo.Uri))
                {
                    var uriParts = videoInfo.Uri.Split('/');
                    if (uriParts.Length > 0)
                    {
                        var lastPart = uriParts.Last();
                        if (lastPart.StartsWith("Reel_") && int.TryParse(lastPart.Replace("Reel_", ""), out int id))
                            return id;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract reel ID from Vimeo video {VideoId}", webhook.VideoId);
            }

            return null;
        }

        private async Task ProcessVideoCreated(Reel reel, VimeoWebhook webhook)
        {
            reel.UploadStatus = "processing";
            reel.VimeoVideoId = webhook.VideoId;

            await LogToDatabase("INFO", "video.created", reel.Id,
                "Video created on Vimeo",
                $"VimeoVideoId: {webhook.VideoId}, Status: processing");
        }

        private async Task ProcessTranscodeComplete(Reel reel, VimeoWebhook webhook)
        {
            reel.UploadStatus = "transcoding_complete";

            await LogToDatabase("INFO", "video.transcode.complete", reel.Id,
                "Video transcoding complete",
                $"VimeoVideoId: {webhook.VideoId}");
        }

        private async Task ProcessTranscodePlayable(Reel reel, VimeoWebhook webhook)
        {
            // Video is partially playable (lower quality)
            reel.UploadStatus = "partially_ready";

            try
            {
                // Fetch video info to update URLs
                var videoInfo = await _vimeoService.GetVideoAsync(webhook.VideoId);
                if (videoInfo != null)
                {
                    reel.VimeoPlayerUrl = videoInfo.PlayerEmbedUrl;
                    reel.VimeoEmbedHtml = videoInfo.Embed?.Html;

                    // Get thumbnail
                    if (videoInfo.Pictures?.Sizes?.Count > 0)
                    {
                        var thumbnail = videoInfo.Pictures.Sizes.FirstOrDefault();
                        reel.ThumbnailUrl = thumbnail?.Link ?? "pending";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching video info in transcode.playable for reel {ReelId}", reel.Id);
            }

            await LogToDatabase("INFO", "video.transcode.playable", reel.Id,
                "Video is partially playable",
                $"VimeoVideoId: {webhook.VideoId}, Player URL: {reel.VimeoPlayerUrl}");
        }

        private async Task ProcessTranscodeFullyPlayable(Reel reel, VimeoWebhook webhook)
        {
            // Video is fully processed and ready
            try
            {
                // Fetch complete video info
                var videoInfo = await _vimeoService.GetVideoAsync(webhook.VideoId);

                if (videoInfo == null)
                {
                    await LogToDatabase("ERROR", "video.transcode.fully.playable", reel.Id,
                        "Failed to fetch video info from Vimeo API",
                        $"VideoId: {webhook.VideoId}");
                    return;
                }

                // Update reel with Vimeo info
                reel.UploadStatus = "ready";
                reel.VimeoVideoId = videoInfo.VideoId;
                reel.VimeoPlayerUrl = videoInfo.PlayerEmbedUrl;
                reel.VimeoEmbedHtml = videoInfo.Embed?.Html;
                reel.DurationInSeconds = (int)Math.Ceiling(videoInfo.Duration);
                reel.VideoUrl = videoInfo.PlayerEmbedUrl; // Use player URL for playback

                // Get the best thumbnail
                if (videoInfo.Pictures?.Sizes?.Count > 0)
                {
                    // Get medium-sized thumbnail (640px or larger)
                    var thumbnail = videoInfo.Pictures.Sizes
                        .OrderBy(s => s.Width)
                        .FirstOrDefault(s => s.Width >= 640)
                        ?? videoInfo.Pictures.Sizes.OrderByDescending(s => s.Width).First();

                    reel.ThumbnailUrl = thumbnail?.Link;
                }

                await LogToDatabase("INFO", "video.transcode.fully.playable", reel.Id,
                    "Video is fully playable and ready",
                    $"✅ FINAL - VimeoVideoId: {reel.VimeoVideoId}\n" +
                    $"✅ FINAL - Player URL: {reel.VimeoPlayerUrl}\n" +
                    $"✅ FINAL - Duration: {reel.DurationInSeconds}s\n" +
                    $"✅ FINAL - Thumbnail: {reel.ThumbnailUrl}");
            }
            catch (Exception ex)
            {
                await LogToDatabase("ERROR", "video.transcode.fully.playable", reel.Id,
                    "Error processing fully playable video",
                    ex.ToString());
                throw;
            }
        }

        private async Task ProcessUploadFailed(Reel reel, VimeoWebhook webhook)
        {
            reel.UploadStatus = "failed";
            reel.UploadError = $"Vimeo upload failed: {webhook.Error?.Message ?? "Unknown error"}";

            await LogToDatabase("ERROR", "video.upload.failed", reel.Id,
                "Video upload failed",
                $"VimeoVideoId: {webhook.VideoId}, Error: {reel.UploadError}");
        }

        private async Task ProcessVideoDeleted(Reel reel, VimeoWebhook webhook)
        {
            reel.UploadStatus = "deleted";
            reel.VimeoVideoId = null;
            reel.VimeoPlayerUrl = null;
            reel.VideoUrl = "deleted";

            await LogToDatabase("INFO", "video.deleted", reel.Id,
                "Video deleted from Vimeo",
                $"VimeoVideoId: {webhook.VideoId}");
        }

        private async Task ProcessVideoUpdated(Reel reel, VimeoWebhook webhook)
        {
            // Video metadata was updated
            try
            {
                var videoInfo = await _vimeoService.GetVideoAsync(webhook.VideoId);
                if (videoInfo != null)
                {
                    // Update any changed metadata
                    reel.VimeoPlayerUrl = videoInfo.PlayerEmbedUrl;
                    reel.VimeoEmbedHtml = videoInfo.Embed?.Html;

                    await LogToDatabase("INFO", "video.updated", reel.Id,
                        "Video metadata updated",
                        $"VimeoVideoId: {webhook.VideoId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating video info for reel {ReelId}", reel.Id);
            }
        }

        private async Task ProcessThumbnailAvailable(Reel reel, VimeoWebhook webhook)
        {
            // Automatic thumbnail is available
            try
            {
                var videoInfo = await _vimeoService.GetVideoAsync(webhook.VideoId);
                if (videoInfo?.Pictures?.Sizes?.Count > 0)
                {
                    var thumbnail = videoInfo.Pictures.Sizes
                        .OrderBy(s => s.Width)
                        .FirstOrDefault(s => s.Width >= 640);

                    reel.ThumbnailUrl = thumbnail?.Link ?? videoInfo.Pictures.Sizes.First().Link;

                    await LogToDatabase("INFO", "automatic-thumbnail-available", reel.Id,
                        "Automatic thumbnail available",
                        $"Thumbnail URL: {reel.ThumbnailUrl}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing thumbnail for reel {ReelId}", reel.Id);
            }
        }

        // Vimeo Webhook Models
        public class VimeoWebhook
        {
            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("video_id")]
            public string VideoId { get; set; }

            [JsonPropertyName("metadata")]
            public VimeoWebhookMetadata Metadata { get; set; }

            [JsonPropertyName("error")]
            public VimeoError Error { get; set; }

            [JsonPropertyName("timestamp")]
            public long Timestamp { get; set; }
        }

        public class VimeoWebhookMetadata
        {
            [JsonPropertyName("reel_id")]
            public string ReelId { get; set; }

            [JsonPropertyName("user_id")]
            public string UserId { get; set; }
        }

        public class VimeoError
        {
            [JsonPropertyName("code")]
            public string Code { get; set; }

            [JsonPropertyName("message")]
            public string Message { get; set; }
        }
    }
}