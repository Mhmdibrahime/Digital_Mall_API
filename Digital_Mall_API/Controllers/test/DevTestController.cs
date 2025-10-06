using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.Entities.Reels___Content;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Digital_Mall_API.Controllers
{
    [ApiController]
    [Route("api/dev")]
    public class DevTestController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DevTestController> _logger;

        public DevTestController(AppDbContext context, ILogger<DevTestController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Test endpoint to simulate a successful upload
        [HttpPost("simulate-success/{reelId}")]
        public async Task<IActionResult> SimulateSuccess(int reelId)
        {
            var reel = await _context.Reels.FindAsync(reelId);
            if (reel == null) return NotFound($"Reel {reelId} not found");

            // Simulate "video.asset.ready" webhook
            reel.UploadStatus = "ready";
            reel.MuxAssetId = "test_asset_" + Guid.NewGuid();
            reel.MuxPlaybackId = "test_playback_" + Guid.NewGuid();
            reel.VideoUrl = $"https://stream.mux.com/{reel.MuxPlaybackId}.m3u8";
            reel.ThumbnailUrl = $"https://image.mux.com/{reel.MuxPlaybackId}/thumbnail.jpg";
            reel.DurationInSeconds = 45; // Example duration

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Simulated successful upload",
                reelId = reel.Id,
                status = reel.UploadStatus,
                videoUrl = reel.VideoUrl
            });
        }

        // Test endpoint to simulate upload failure
        [HttpPost("simulate-failure/{reelId}")]
        public async Task<IActionResult> SimulateFailure(int reelId)
        {
            var reel = await _context.Reels.FindAsync(reelId);
            if (reel == null) return NotFound($"Reel {reelId} not found");

            // Simulate "video.asset.errored" webhook
            reel.UploadStatus = "failed";
            reel.UploadError = "Test simulation: Video processing failed";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Simulated upload failure",
                reelId = reel.Id,
                status = reel.UploadStatus,
                error = reel.UploadError
            });
        }

        // Get reel status
        [HttpGet("reel-status/{reelId}")]
        public async Task<IActionResult> GetReelStatus(int reelId)
        {
            var reel = await _context.Reels
                .Include(r => r.PostedByBrand)
                .Include(r => r.PostedByFashionModel)
                .Include(r => r.LinkedProducts)
                .FirstOrDefaultAsync(r => r.Id == reelId);

            if (reel == null) return NotFound();

            return Ok(new
            {
                id = reel.Id,
                status = reel.UploadStatus,
                videoUrl = reel.VideoUrl,
                thumbnailUrl = reel.ThumbnailUrl,
                muxAssetId = reel.MuxAssetId,
                muxPlaybackId = reel.MuxPlaybackId,
                error = reel.UploadError,
                duration = reel.DurationInSeconds,
                caption = reel.Caption
            });
        }
    }
}