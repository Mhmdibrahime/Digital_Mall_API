using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.DTOs.UserDTOs;
using Digital_Mall_API.Models.Entities.Product_Catalog;
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
    public class BrandProfileController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public BrandProfileController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        [HttpGet("brand-details/{brandId}")]
        public async Task<IActionResult> GetBrandDetails(string brandId)
        {
            var brand = await _context.Brands
                .FirstOrDefaultAsync(b => b.Id == brandId);

            if (brand == null)
                return NotFound(new { message = "Brand not found" });

            var followersCount = await _context.FollowingBrands
                .CountAsync(f => f.BrandId == brandId);

            var productsCount = await _context.Products
                .CountAsync(p => p.BrandId == brandId && p.IsActive);

            var reelsCount = await _context.Reels
                .CountAsync(r => r.PostedByBrandId == brandId);

            var totalLikes = await _context.Reels
                .Where(r => r.PostedByBrandId == brandId)
                .SumAsync(r => r.LikesCount);

            var currentUserId = GetCurrentUserId();
            var isFollowing = false;
            if (!string.IsNullOrEmpty(currentUserId))
            {
                isFollowing = await _context.FollowingBrands
                    .AnyAsync(f => f.BrandId == brandId && f.CustomerId == currentUserId);
            }

            var brandDetails = new
            {
                BrandId = brand.Id,
                BrandName = brand.OfficialName,
                Description = brand.Description,
                LogoUrl = brand.LogoUrl,
                Location = brand.Location,
                FollowersCount = followersCount,
                ProductsCount = productsCount,
                ReelsCount = reelsCount,
                TotalLikes = totalLikes,
                IsFollowing = isFollowing,
                SocialMedia = new
                {
                    Facebook = brand.Facebook,
                    Instagram = brand.Instgram
                }
            };

            return Ok(brandDetails);
        }

        [HttpGet("brand-products/{brandId}")]
        public async Task<IActionResult> GetBrandProducts(
            string brandId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12)
        {
            var brand = await _context.Brands.FindAsync(brandId);
            if (brand == null)
                return NotFound(new { message = "Brand not found" });

            var query = _context.Products
                .Where(p => p.BrandId == brandId && p.IsActive)
                .Include(p => p.Images)
                .Include(p => p.ProductDiscount)
                .Include(p => p.Brand)
                .AsQueryable();

            var totalCount = await query.CountAsync();

            var products = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Description,
                    OriginalPrice = p.Price,
                    DiscountedPrice = p.ProductDiscount != null ?
                        p.Price - (p.Price*(p.ProductDiscount.DiscountValue/100)) : p.Price,
                    HasDiscount = p.ProductDiscount != null,
                    MainImage = p.Images.FirstOrDefault().ImageUrl,
                    
                    BrandName = p.Brand.OfficialName,
                    StockQuantity = p.Variants.Sum(v => v.StockQuantity),

                    p.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Products = products
            });
        }

        [HttpGet("brand-reels/{brandId}")]
        public async Task<IActionResult> GetBrandReels(
            string brandId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12)
        {
            var brand = await _context.Brands.FindAsync(brandId);
            if (brand == null)
                return NotFound(new { message = "Brand not found" });

            var query = _context.Reels
                .Where(r => r.PostedByBrandId == brandId && r.UploadStatus == "completed")
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
                        ImageUrl = rp.Product.Images.FirstOrDefault().ImageUrl
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

        [HttpPost("follow-brand/{brandId}")]
        public async Task<IActionResult> FollowBrand(string brandId)
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized(new { message = "User not authenticated" });

            var brand = await _context.Brands.FindAsync(brandId);
            if (brand == null)
                return NotFound(new { message = "Brand not found" });

            var existingFollow = await _context.FollowingBrands
                .FirstOrDefaultAsync(f => f.BrandId == brandId && f.CustomerId == currentUserId);

            if (existingFollow != null)
                return BadRequest(new { message = "Already following this brand" });

            var follow = new FollowingBrand
            {
                CustomerId = currentUserId,
                BrandId = brandId,
                FollowedAt = DateTime.UtcNow
            };

            _context.FollowingBrands.Add(follow);
            await _context.SaveChangesAsync();

            var followersCount = await _context.FollowingBrands
                .CountAsync(f => f.BrandId == brandId);

            return Ok(new
            {
                message = "Brand followed successfully",
                followersCount = followersCount,
                isFollowing = true
            });
        }

        [HttpPost("unfollow-brand/{brandId}")]
        public async Task<IActionResult> UnfollowBrand(string brandId)
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized(new { message = "User not authenticated" });

            var brand = await _context.Brands.FindAsync(brandId);
            if (brand == null)
                return NotFound(new { message = "Brand not found" });

            var existingFollow = await _context.FollowingBrands
                .FirstOrDefaultAsync(f => f.BrandId == brandId && f.CustomerId == currentUserId);

            if (existingFollow == null)
                return BadRequest(new { message = "Not following this brand" });

            _context.FollowingBrands.Remove(existingFollow);
            await _context.SaveChangesAsync();

            var followersCount = await _context.FollowingBrands
                .CountAsync(f => f.BrandId == brandId);

            return Ok(new
            {
                message = "Brand unfollowed successfully",
                followersCount = followersCount,
                isFollowing = false
            });
        }

        [HttpPost("toggle-follow/{brandId}")]
        public async Task<IActionResult> ToggleFollowBrand(string brandId)
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized(new { message = "User not authenticated" });

            var brand = await _context.Brands.FindAsync(brandId);
            if (brand == null)
                return NotFound(new { message = "Brand not found" });

            var existingFollow = await _context.FollowingBrands
                .FirstOrDefaultAsync(f => f.BrandId == brandId && f.CustomerId == currentUserId);

            if (existingFollow != null)
            {
                // Unfollow
                _context.FollowingBrands.Remove(existingFollow);
                await _context.SaveChangesAsync();
            }
            else
            {
                // Follow
                var follow = new FollowingBrand
                {
                    CustomerId = currentUserId,
                    BrandId = brandId,
                    FollowedAt = DateTime.UtcNow
                };
                _context.FollowingBrands.Add(follow);
                await _context.SaveChangesAsync();
            }

            // Get updated data
            var followersCount = await _context.FollowingBrands
                .CountAsync(f => f.BrandId == brandId);

            var isFollowing = existingFollow == null; // If existing was null, now we're following

            return Ok(new
            {
                message = isFollowing ? "Brand followed successfully" : "Brand unfollowed successfully",
                followersCount = followersCount,
                isFollowing = isFollowing
            });
        }

        [HttpGet("is-following/{brandId}")]
        public async Task<IActionResult> IsFollowingBrand(string brandId)
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized(new { message = "User not authenticated" });

            var isFollowing = await _context.FollowingBrands
                .AnyAsync(f => f.BrandId == brandId && f.CustomerId == currentUserId);

            return Ok(new { isFollowing });
        }
    }
}