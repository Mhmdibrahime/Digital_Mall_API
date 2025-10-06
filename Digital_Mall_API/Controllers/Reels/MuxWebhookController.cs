using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.Entities.Reels___Content;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

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

        [HttpPost]
        public async Task<IActionResult> HandleMuxWebhook()
        {
            try
            {
                using var reader = new StreamReader(HttpContext.Request.Body);
                var requestBody = await reader.ReadToEndAsync();

                _logger.LogInformation("Received Mux webhook: {RequestBody}", requestBody);

                var webhook = JsonSerializer.Deserialize<MuxWebhook>(requestBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (webhook?.Data?.Passthrough == null || !int.TryParse(webhook.Data.Passthrough, out int reelId))
                {
                    _logger.LogWarning("Webhook missing or invalid passthrough ID: {Passthrough}", webhook?.Data?.Passthrough);
                    return BadRequest("Invalid passthrough ID");
                }

                var reel = await _context.Reels.FindAsync(reelId);
                if (reel == null)
                {
                    _logger.LogWarning("Reel not found for passthrough: {Passthrough}", webhook.Data.Passthrough);
                    return NotFound($"Reel {reelId} not found");
                }

                switch (webhook.Type)
                {
                    case "video.upload.asset_created":
                        reel.MuxAssetId = webhook.Data.Id;
                        reel.UploadStatus = "processing";
                        _logger.LogInformation("Asset created for reel {ReelId}, Mux asset ID: {MuxAssetId}",
                            reel.Id, webhook.Data.Id);
                        break;

                    case "video.asset.ready":
                        reel.MuxAssetId = webhook.Data.Id;
                        reel.MuxPlaybackId = webhook.Data.PlaybackIds?.FirstOrDefault()?.Id;
                        reel.UploadStatus = "ready";
                        reel.DurationInSeconds = (int)Math.Ceiling(webhook.Data.Duration ?? 0);

                        if (!string.IsNullOrEmpty(reel.MuxPlaybackId))
                        {
                            reel.VideoUrl = $"https://stream.mux.com/{reel.MuxPlaybackId}.m3u8";
                            reel.ThumbnailUrl = $"https://image.mux.com/{reel.MuxPlaybackId}/thumbnail.jpg";
                        }

                        _logger.LogInformation("Reel {ReelId} ready for playback. Duration: {Duration}s, Playback ID: {PlaybackId}",
                            reel.Id, reel.DurationInSeconds, reel.MuxPlaybackId);
                        break;

                    case "video.asset.errored":
                        reel.UploadStatus = "failed";
                        reel.UploadError = webhook.Data.Errors?.Message ?? "Unknown error";
                        _logger.LogError("Asset processing failed for reel {ReelId}: {Error}",
                            reel.Id, reel.UploadError);
                        break;

                    case "video.upload.cancelled":
                        reel.UploadStatus = "cancelled";
                        _logger.LogInformation("Upload cancelled for reel {ReelId}", reel.Id);
                        break;

                    default:
                        _logger.LogInformation("Received unhandled Mux webhook type: {WebhookType} for reel {ReelId}",
                            webhook.Type, reel.Id);
                        break;
                }

                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing Mux webhook");
                return StatusCode(500, "Internal server error");
            }
        }
    }

    // Webhook DTOs
    public class MuxWebhook
    {
        public string Type { get; set; }
        public MuxWebhookData Data { get; set; }
    }

    public class MuxWebhookData
    {
        public string Id { get; set; }
        public string Passthrough { get; set; }
        public List<MuxPlaybackId> PlaybackIds { get; set; }
        public double? Duration { get; set; }
        public MuxError Errors { get; set; }
    }

    public class MuxPlaybackId
    {
        public string Id { get; set; }
        public string Policy { get; set; }
    }

    public class MuxError
    {
        public string Type { get; set; }
        public string Message { get; set; }
    }
}