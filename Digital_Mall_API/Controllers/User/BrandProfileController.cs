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

            if (brand == null || brand.Status != "Active")
                return NotFound(new { message = "Brand not found" });

            var followersCount = await _context.FollowingBrands
                .CountAsync(f => f.BrandId == brandId);

            var productsCount = await _context.Products
                .CountAsync(p => p.BrandId == brandId && p.IsActive);

            var reelsCount = await _context.Reels.Where(r => r.UploadStatus == "ready")
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
                IsCertified = brand.IsCertified,
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
                .Where(p => p.Variants.Any(v => v.StockQuantity > 0) && p.IsActive && p.Brand.Status == "Active")
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
                .Where(r => r.PostedByBrandId == brandId && r.UploadStatus == "ready")
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
                    Products = r.LinkedProducts.Where(p=>p.Product.IsActive == true).Select(rp => new
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
        [HttpGet("brand-feedbacks/{brandId}")]
        public async Task<IActionResult> GetBrandFeedbacks(
    string brandId,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] int? rating = null,
    [FromQuery] string sortBy = "newest") // newest, oldest, highest-rating, lowest-rating
        {
            // Check if brand exists
            var brand = await _context.Brands
                .FirstOrDefaultAsync(b => b.Id == brandId && b.Status == "Active");
            if (brand == null)
                return NotFound(new { message = "Brand not found" });

            // Base query - get all feedbacks for products belonging to this brand
            var query = _context.ProductFeedbacks
                .Include(f => f.Product)
                .Include(f => f.User)
                .Where(f => f.Product.BrandId == brandId && f.Product.IsActive)
                .AsQueryable();

            // Apply rating filter if provided
            if (rating.HasValue)
            {
                query = query.Where(f => f.Rating == rating.Value);
            }

            // Apply sorting
            query = sortBy.ToLower() switch
            {
                "oldest" => query.OrderBy(f => f.CreatedAt),
                "highest-rating" => query.OrderByDescending(f => f.Rating).ThenByDescending(f => f.CreatedAt),
                "lowest-rating" => query.OrderBy(f => f.Rating).ThenByDescending(f => f.CreatedAt),
                _ => query.OrderByDescending(f => f.CreatedAt) // "newest" is default
            };

            // Get total count for pagination
            var totalCount = await query.CountAsync();

            // Get rating statistics
            var ratingStats = await _context.ProductFeedbacks
                .Where(f => f.Product.BrandId == brandId && f.Product.IsActive)
                .GroupBy(f => 1)
                .Select(g => new
                {
                    AverageRating = g.Average(f => f.Rating),
                    TotalReviews = g.Count(),
                    RatingDistribution = new
                    {
                        Rating5 = g.Count(f => f.Rating == 5),
                        Rating4 = g.Count(f => f.Rating == 4),
                        Rating3 = g.Count(f => f.Rating == 3),
                        Rating2 = g.Count(f => f.Rating == 2),
                        Rating1 = g.Count(f => f.Rating == 1)
                    }
                })
                .FirstOrDefaultAsync() ?? new
                {
                    AverageRating = 0.0,
                    TotalReviews = 0,
                    RatingDistribution = new
                    {
                        Rating5 = 0,
                        Rating4 = 0,
                        Rating3 = 0,
                        Rating2 = 0,
                        Rating1 = 0
                    }
                };

            // Apply pagination
            var feedbacks = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new
                {
                    f.Id,
                    f.Rating,
                    f.Comment,
                    f.ImageUrl,
                    f.CreatedAt,
                    Product = new
                    {
                        f.Product.Id,
                        f.Product.Name,
                        MainImage = f.Product.Images.FirstOrDefault().ImageUrl
                    },
                    User = new
                    {
                        f.User.Id,
                        f.User.FullName
                    }
                })
                .ToListAsync();

            return Ok(new
            {
                BrandId = brandId,
                BrandName = brand.OfficialName,
                Statistics = new
                {
                    ratingStats.AverageRating,
                    ratingStats.TotalReviews,
                    ratingStats.RatingDistribution
                },
                Pagination = new
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    HasNextPage = page < (int)Math.Ceiling(totalCount / (double)pageSize),
                    HasPreviousPage = page > 1
                },
                Filters = new
                {
                    AppliedRating = rating,
                    AppliedSort = sortBy
                },
                Feedbacks = feedbacks
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