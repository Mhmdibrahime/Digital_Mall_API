using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.DTOs.UserDTOs;
using Digital_Mall_API.Models.Entities.Reels___Content;
using Digital_Mall_API.Models.Entities.User___Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Digital_Mall_API.Controllers.User
{
    [Route("User/[controller]")]
    [ApiController]
    public class ModelProfileController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ModelProfileController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        [HttpGet("model-details/{modelId}")]
        public async Task<IActionResult> GetModelDetails(string modelId)
        {
            var model = await _context.FashionModels
                .FirstOrDefaultAsync(m => m.Id == modelId);

            if (model == null)
                return NotFound(new { message = "Model not found" });

            var followersCount = await _context.FollowingModels
                .CountAsync(f => f.FashionModelId == modelId);

            var reelsCount = await _context.Reels
                .CountAsync(r => r.PostedByModelId == modelId && r.UploadStatus == "ready");

            var totalLikes = await _context.Reels
                .Where(r => r.PostedByModelId == modelId)
                .SumAsync(r => r.LikesCount);

            var currentUserId = GetCurrentUserId();
            var isFollowing = false;
            if (!string.IsNullOrEmpty(currentUserId))
            {
                isFollowing = await _context.FollowingModels
                    .AnyAsync(f => f.FashionModelId == modelId && f.CustomerId == currentUserId);
            }

            var modelDetails = new
            {
                ModelId = model.Id,
                Name = model.Name,
                Bio = model.Bio,
                ImageUrl = model.ImageUrl,
                FollowersCount = followersCount,
                ReelsCount = reelsCount,
                TotalLikes = totalLikes,
                IsFollowing = isFollowing,
                SocialMedia = new
                {
                    Facebook = model.Facebook,
                    Instagram = model.Instgram,
                    Other = model.OtherSocialAccount
                }
            };

            return Ok(modelDetails);
        }

        [HttpGet("model-reels/{modelId}")]
        public async Task<IActionResult> GetModelReels(
            string modelId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 8)
        {
            var model = await _context.FashionModels.FindAsync(modelId);
            if (model == null)
                return NotFound(new { message = "Model not found" });

            var query = _context.Reels
                .Where(r => r.PostedByModelId == modelId && r.UploadStatus == "ready")
                .Include(r => r.LinkedProducts)
                    .ThenInclude(rp => rp.Product)
                        .ThenInclude(p => p.Images)
                .AsQueryable();

            var totalCount = await query.CountAsync();

            var reels = await query
                .OrderByDescending(r => r.PostedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new
                {
                    r.Id,
                    r.VideoUrl,
                    r.ThumbnailUrl,
                    r.Caption,
                    r.LikesCount,
                    r.SharesCount,
                    r.PostedDate,
                    r.DurationInSeconds,
                    Products = r.LinkedProducts.Select(rp => new
                    {
                        rp.Product.Id,
                        rp.Product.Name,
                        rp.Product.Price,
                        ImageUrl = rp.Product.Images.FirstOrDefault().ImageUrl,
                        BrandName = rp.Product.Brand.OfficialName
                    }).ToList()
                })
                .ToListAsync();

            return Ok(new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Reels = reels
            });
        }

        [HttpPost("follow-model/{modelId}")]
        public async Task<IActionResult> FollowModel(string modelId)
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized(new { message = "User not authenticated" });

            var model = await _context.FashionModels.FindAsync(modelId);
            if (model == null)
                return NotFound(new { message = "Model not found" });

            var existingFollow = await _context.FollowingModels
                .FirstOrDefaultAsync(f => f.FashionModelId == modelId && f.CustomerId == currentUserId);

            if (existingFollow != null)
                return BadRequest(new { message = "Already following this model" });

            var follow = new FollowingModel
            {
                CustomerId = currentUserId,
                FashionModelId = modelId,
                FollowedAt = DateTime.UtcNow
            };

            _context.FollowingModels.Add(follow);
            await _context.SaveChangesAsync();

            var followersCount = await _context.FollowingModels
                .CountAsync(f => f.FashionModelId == modelId);

            return Ok(new
            {
                message = "Model followed successfully",
                followersCount = followersCount,
                isFollowing = true
            });
        }

        [HttpPost("unfollow-model/{modelId}")]
        public async Task<IActionResult> UnfollowModel(string modelId)
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized(new { message = "User not authenticated" });

            var model = await _context.FashionModels.FindAsync(modelId);
            if (model == null)
                return NotFound(new { message = "Model not found" });

            var existingFollow = await _context.FollowingModels
                .FirstOrDefaultAsync(f => f.FashionModelId == modelId && f.CustomerId == currentUserId);

            if (existingFollow == null)
                return BadRequest(new { message = "Not following this model" });

            _context.FollowingModels.Remove(existingFollow);
            await _context.SaveChangesAsync();

            var followersCount = await _context.FollowingModels
                .CountAsync(f => f.FashionModelId == modelId);

            return Ok(new
            {
                message = "Model unfollowed successfully",
                followersCount = followersCount,
                isFollowing = false
            });
        }

        [HttpPost("toggle-follow-model/{modelId}")]
        public async Task<IActionResult> ToggleFollowModel(string modelId)
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized(new { message = "User not authenticated" });

            var model = await _context.FashionModels.FindAsync(modelId);
            if (model == null)
                return NotFound(new { message = "Model not found" });

            var existingFollow = await _context.FollowingModels
                .FirstOrDefaultAsync(f => f.FashionModelId == modelId && f.CustomerId == currentUserId);

            if (existingFollow != null)
            {
                _context.FollowingModels.Remove(existingFollow);
                await _context.SaveChangesAsync();
            }
            else
            {
                var follow = new FollowingModel
                {
                    CustomerId = currentUserId,
                    FashionModelId = modelId,
                    FollowedAt = DateTime.UtcNow
                };
                _context.FollowingModels.Add(follow);
                await _context.SaveChangesAsync();
            }

            var followersCount = await _context.FollowingModels
                .CountAsync(f => f.FashionModelId == modelId);

            var isFollowing = existingFollow == null; 

            return Ok(new
            {
                message = isFollowing ? "Model followed successfully" : "Model unfollowed successfully",
                followersCount = followersCount,
                isFollowing = isFollowing
            });
        }

        [HttpGet("is-following/{modelId}")]
        public async Task<IActionResult> IsFollowingModel(string modelId)
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized(new { message = "User not authenticated" });

            var isFollowing = await _context.FollowingModels
                .AnyAsync(f => f.FashionModelId == modelId && f.CustomerId == currentUserId);

            return Ok(new { isFollowing });
        }

        //[HttpGet("popular-models")]
        //public async Task<IActionResult> GetPopularModels(
        //    [FromQuery] int page = 1,
        //    [FromQuery] int pageSize = 12)
        //{
        //    var query = _context.FashionModels
        //        .Where(m => m.Status == "Approved") // Only approved models
        //        .Select(m => new
        //        {
        //            m.Id,
        //            m.Name,
        //            m.Bio,
        //            m.ImageUrl,
        //            FollowersCount = m.Followers.Count,
        //            ReelsCount = m.Reels.Count(r => r.UploadStatus == "completed"),
        //            TotalLikes = m.Reels.Sum(r => r.LikesCount)
        //        })
        //        .OrderByDescending(m => m.FollowersCount)
        //        .ThenByDescending(m => m.TotalLikes)
        //        .AsQueryable();

        //    var totalCount = await query.CountAsync();

        //    var models = await query
        //        .Skip((page - 1) * pageSize)
        //        .Take(pageSize)
        //        .ToListAsync();

        //    return Ok(new
        //    {
        //        TotalCount = totalCount,
        //        Page = page,
        //        PageSize = pageSize,
        //        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
        //        Models = models
        //    });
        //}
    }
}