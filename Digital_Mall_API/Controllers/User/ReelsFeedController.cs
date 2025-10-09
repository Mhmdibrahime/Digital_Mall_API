using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.Entities;
using Digital_Mall_API.Models.DTOs.UserDTOs.ReelsDTOs;
using Digital_Mall_API.Models.Entities.Reels___Content;
using Digital_Mall_API.Models.Entities.User___Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Digital_Mall_API.Controllers.Reels
{
    [ApiController]
    [Route("User/reels-feed")]
    public class ReelsFeedController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ReelsFeedController> _logger;

        public ReelsFeedController(AppDbContext context, ILogger<ReelsFeedController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("for-you")]
        public async Task<ActionResult<List<ReelFeedDto>>> GetForYouReels(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var customerId = GetCurrentCustomerId();
                if (string.IsNullOrEmpty(customerId))
                {
                    return Unauthorized("User not authenticated");
                }

                var baseQuery = _context.Reels
                    .Where(r => r.UploadStatus == "ready")
                    .Include(r => r.PostedByFashionModel)
                    .Include(r => r.PostedByBrand)
                    .Include(r => r.LinkedProducts)
                        .ThenInclude(rp => rp.Product)
                    .AsQueryable();

                var forYouReels = await GetForYouAlgorithmReels(baseQuery, customerId, page, pageSize);

                var reelIds = forYouReels.Select(r => r.Id).ToList();
                var userLikes = await _context.ReelLikes
                    .Where(l => l.CustomerId == customerId && reelIds.Contains(l.ReelId))
                    .Select(l => l.ReelId)
                    .ToListAsync();

                var reelDtos = forYouReels.Select(reel => new ReelFeedDto
                {
                    Id = reel.Id,
                    Caption = reel.Caption,
                    VideoUrl = reel.VideoUrl,
                    ThumbnailUrl = reel.ThumbnailUrl,
                    PostedDate = reel.PostedDate,
                    DurationInSeconds = reel.DurationInSeconds,
                    LikesCount = reel.LikesCount,
                    SharesCount = reel.SharesCount,
                    IsLikedByCurrentUser = userLikes.Contains(reel.Id),
                    PostedByUserType = reel.PostedByUserType,
                    PostedByName = reel.PostedByUserType == "FashionModel"
                        ? reel.PostedByFashionModel.Name
                        : reel.PostedByBrand.OfficialName,
                    PostedByImage = reel.PostedByUserType == "FashionModel"
                        ? reel.PostedByFashionModel.ImageUrl
                        : reel.PostedByBrand.LogoUrl,
                    LinkedProducts = reel.LinkedProducts.Select(rp => new ReelProductDto
                    {
                        ProductId = rp.ProductId,
                        ProductName = rp.Product.Name,
                        ProductPrice = rp.Product.Price,
                        ProductImageUrl = rp.Product.Images.FirstOrDefault()?.ImageUrl
                    }).ToList()
                }).ToList();

                return Ok(reelDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting for-you reels");
                return StatusCode(500, "Error retrieving reels feed");
            }
        }

        [HttpGet("following")]
        public async Task<ActionResult<List<ReelFeedDto>>> GetFollowingReels(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var customerId = GetCurrentCustomerId();
                if (string.IsNullOrEmpty(customerId))
                {
                    return Unauthorized("User not authenticated");
                }

                var followedBrands = await _context.FollowingBrands
                    .Where(fb => fb.CustomerId == customerId)
                    .Select(fb => fb.BrandId)
                    .ToListAsync();

                var followedModels = await _context.FollowingModels
                    .Where(fm => fm.CustomerId == customerId)
                    .Select(fm => fm.FashionModelId)
                    .ToListAsync();

                var followingReels = await _context.Reels
                    .Where(r => r.UploadStatus == "ready" &&
                               ((r.PostedByUserType == "Brand" && followedBrands.Contains(r.PostedByUserId)) ||
                                (r.PostedByUserType == "FashionModel" && followedModels.Contains(r.PostedByUserId))))
                    .Include(r => r.PostedByFashionModel)
                    .Include(r => r.PostedByBrand)
                    .Include(r => r.LinkedProducts)
                        .ThenInclude(rp => rp.Product)
                    .OrderByDescending(r => r.PostedDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var reelIds = followingReels.Select(r => r.Id).ToList();
                var userLikes = await _context.ReelLikes
                    .Where(l => l.CustomerId == customerId && reelIds.Contains(l.ReelId))
                    .Select(l => l.ReelId)
                    .ToListAsync();

                var reelDtos = followingReels.Select(reel => new ReelFeedDto
                {
                    Id = reel.Id,
                    Caption = reel.Caption,
                    VideoUrl = reel.VideoUrl,
                    ThumbnailUrl = reel.ThumbnailUrl,
                    PostedDate = reel.PostedDate,
                    DurationInSeconds = reel.DurationInSeconds,
                    LikesCount = reel.LikesCount,
                    SharesCount = reel.SharesCount,
                    IsLikedByCurrentUser = userLikes.Contains(reel.Id),
                    PostedByUserType = reel.PostedByUserType,
                    PostedByName = reel.PostedByUserType == "FashionModel"
                        ? reel.PostedByFashionModel.Name
                        : reel.PostedByBrand.OfficialName,
                    PostedByImage = reel.PostedByUserType == "FashionModel"
                        ? reel.PostedByFashionModel.ImageUrl
                        : reel.PostedByBrand.LogoUrl,
                    LinkedProducts = reel.LinkedProducts.Select(rp => new ReelProductDto
                    {
                        ProductId = rp.ProductId,
                        ProductName = rp.Product.Name,
                        ProductPrice = rp.Product.Price,
                        ProductImageUrl = rp.Product.Images.FirstOrDefault()?.ImageUrl
                    }).ToList()
                }).ToList();

                return Ok(reelDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting following reels");
                return StatusCode(500, "Error retrieving following reels");
            }
        }

        [HttpPost("like")]
        public async Task<ActionResult<ReelInteractionResponse>> LikeReel([FromBody] LikeReelRequest request)
        {
            try
            {
                var customerId = GetCurrentCustomerId();
                if (string.IsNullOrEmpty(customerId))
                {
                    return Unauthorized("User not authenticated");
                }

                var reel = await _context.Reels.FindAsync(request.ReelId);
                if (reel == null)
                {
                    return NotFound("Reel not found");
                }

                var existingLike = await _context.ReelLikes
                    .FirstOrDefaultAsync(l => l.ReelId == request.ReelId && l.CustomerId == customerId);

                if (existingLike != null)
                {
                    // Unlike the reel
                    _context.ReelLikes.Remove(existingLike);
                    reel.LikesCount = Math.Max(0, reel.LikesCount - 1);
                }
                else
                {
                    // Like the reel
                    var like = new ReelLike
                    {
                        ReelId = request.ReelId,
                        CustomerId = customerId,
                        LikedAt = DateTime.UtcNow
                    };
                    _context.ReelLikes.Add(like);
                    reel.LikesCount++;
                }

                await _context.SaveChangesAsync();

                var response = new ReelInteractionResponse
                {
                    ReelId = reel.Id,
                    LikesCount = reel.LikesCount,
                    SharesCount = reel.SharesCount,
                    IsLikedByCurrentUser = existingLike == null // If existingLike was null, we just liked it
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error liking/unliking reel {ReelId}", request.ReelId);
                return StatusCode(500, "Error processing like");
            }
        }

        // POST: api/reels-feed/share/{reelId}
        [HttpPost("share/{reelId}")]
        public async Task<ActionResult<ReelInteractionResponse>> ShareReel(int reelId)
        {
            try
            {
                var customerId = GetCurrentCustomerId();
                if (string.IsNullOrEmpty(customerId))
                {
                    return Unauthorized("User not authenticated");
                }

                var reel = await _context.Reels.FindAsync(reelId);
                if (reel == null)
                {
                    return NotFound("Reel not found");
                }

                // Increment share count
                reel.SharesCount++;

                // You might want to track individual shares for analytics
                // For now, we just increment the counter

                await _context.SaveChangesAsync();

                // Check if user likes this reel
                var isLiked = await _context.ReelLikes
                    .AnyAsync(l => l.ReelId == reelId && l.CustomerId == customerId);

                var response = new ReelInteractionResponse
                {
                    ReelId = reel.Id,
                    LikesCount = reel.LikesCount,
                    SharesCount = reel.SharesCount,
                    IsLikedByCurrentUser = isLiked
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sharing reel {ReelId}", reelId);
                return StatusCode(500, "Error processing share");
            }
        }

        // GET: api/reels-feed/{reelId}/likes
        [HttpGet("{reelId}/likes")]
        public async Task<ActionResult> GetReelLikes(int reelId)
        {
            try
            {
                var likes = await _context.ReelLikes
                    .Where(l => l.ReelId == reelId)
                    .Include(l => l.Customer)
                    .Select(l => new
                    {
                        CustomerId = l.CustomerId,
                        CustomerName = l.Customer.FullName,
                        LikedAt = l.LikedAt
                    })
                    .OrderByDescending(l => l.LikedAt)
                    .ToListAsync();

                return Ok(likes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting likes for reel {ReelId}", reelId);
                return StatusCode(500, "Error retrieving likes");
            }
        }

        private string GetCurrentCustomerId()
        {
          var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return userId;
        }

        private async Task<List<Reel>> GetForYouAlgorithmReels(
            IQueryable<Reel> baseQuery,
            string customerId,
            int page,
            int pageSize)
        {
            // Algorithm components:
            // 1. Popularity (likes + shares)
            // 2. Recency
            // 3. Diversity (mix of brands/models)
            // 4. User preferences (based on past interactions)

            var pageStart = (page - 1) * pageSize;
            var halfPageSize = pageSize / 2;

            // Get user's liked reels to understand preferences
            var userLikedReelIds = await _context.ReelLikes
                .Where(l => l.CustomerId == customerId)
                .Select(l => l.ReelId)
                .ToListAsync();

            // Get reels from brands/models user follows (higher priority)
            var followedBrands = await _context.FollowingBrands
                .Where(fb => fb.CustomerId == customerId)
                .Select(fb => fb.BrandId)
                .ToListAsync();

            var followedModels = await _context.FollowingModels
                .Where(fm => fm.CustomerId == customerId)
                .Select(fm => fm.FashionModelId)
                .ToListAsync();

            // Split the feed into different categories for diversity
            var popularReels = await baseQuery
                .Where(r => r.PostedDate >= DateTime.UtcNow.AddDays(-30)) // Last 30 days
                .OrderByDescending(r => (r.LikesCount * 2) + r.SharesCount) // Weighted popularity
                .Take(halfPageSize * 2) // Get more for sampling
                .ToListAsync();

            var recentReels = await baseQuery
                .OrderByDescending(r => r.PostedDate)
                .Take(halfPageSize * 2)
                .ToListAsync();

            var followingReels = await baseQuery
                .Where(r => (r.PostedByUserType == "Brand" && followedBrands.Contains(r.PostedByUserId)) ||
                           (r.PostedByUserType == "FashionModel" && followedModels.Contains(r.PostedByUserId)))
                .OrderByDescending(r => r.PostedDate)
                .Take(halfPageSize)
                .ToListAsync();

            // Combine and shuffle for variety
            var allReels = new List<Reel>();

            // Add following reels first (highest priority)
            allReels.AddRange(followingReels);

            // Add popular reels (avoid duplicates)
            allReels.AddRange(popularReels
                .Where(r => !allReels.Any(ar => ar.Id == r.Id))
                .Take(halfPageSize));

            // Add recent reels (avoid duplicates)
            allReels.AddRange(recentReels
                .Where(r => !allReels.Any(ar => ar.Id == r.Id))
                .Take(pageSize - allReels.Count));

            // If we still need more reels, get random ones
            if (allReels.Count < pageSize)
            {
                var remainingCount = pageSize - allReels.Count;
                var randomReels = await baseQuery
                    .Where(r => !allReels.Any(ar => ar.Id == r.Id))
                    .OrderBy(r => Guid.NewGuid()) // Random order
                    .Take(remainingCount)
                    .ToListAsync();

                allReels.AddRange(randomReels);
            }

            return allReels
                .GroupBy(r => r.Id)
                .Select(g => g.First())
                .Take(pageSize)
                .ToList();
        }
    }
}