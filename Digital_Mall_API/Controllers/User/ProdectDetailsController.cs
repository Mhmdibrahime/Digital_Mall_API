using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.DTOs.UserDTOs;
using Digital_Mall_API.Models.Entities.Product_Catalog;
using Digital_Mall_API.Models.Entities.User___Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Digital_Mall_API.Controllers.User
{
    [Route("User/[controller]")]
    [ApiController]
    public class ProdectDetailsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProdectDetailsController(AppDbContext context,UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("product-details/{id}")]
        public async Task<IActionResult> GetProductDetails(int id)
        {
            var product = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.SubCategory)
                    .ThenInclude(sc => sc.Category)
                .Include(p => p.ProductDiscount)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound(new { message = "Product not found" });

            
            var sizes = product.Variants
                .Select(v => v.Size)
                .Distinct()
                .ToList();

            
            var colors = product.Variants
                .Select(v => v.Color)
                .Distinct()
                .ToList();


            var images = product.Images
                .Select(i => i.ImageUrl)
                .ToList();

            var feedbacksCount = await _context.ProductFeedbacks.CountAsync(f => f.ProductId == id);
            var averageRating = await _context.ProductFeedbacks
                .Where(f => f.ProductId == id)
                .AverageAsync(f => (double?)f.Rating) ?? 0;


            decimal originalPrice = product.Price;
            decimal discountValue = product.ProductDiscount?.DiscountValue ?? 0;
            decimal discountedPrice = originalPrice - discountValue;
            string discountStatus = discountValue > 0 ? "Active" : "None";

           
            var productDetails = new
            {
                product.Id,
                product.Name,
                Description = product.Description,
                BrandName = product.Brand != null ? product.Brand.OfficialName : null,
                Category = product.SubCategory?.Category?.Name,
                SubCategory = product.SubCategory?.Name,
                OriginalPrice = originalPrice,
                DiscountValue = discountValue,
                DiscountedPrice = discountedPrice,
                DiscountStatus = discountStatus,
                AvailableColors = colors,
                AvailableSizes = sizes,
                StockQuantity = product.Variants.Sum(v => v.StockQuantity),
                Images = images,
                FeedBacksCount = feedbacksCount,
                AverageRating = Math.Round(averageRating, 1),
                CreatedAt = product.CreatedAt
            };

            return Ok(productDetails);
        }
        [HttpGet("brand-details/{productId}")]
        public async Task<IActionResult> GetBrandDetailsByProduct(int productId)
        {
            var product = await _context.Products
                .Include(p => p.Brand)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null || product.Brand == null)
                return NotFound(new { message = "Product or Brand not found" });

            var brand = product.Brand;

            var followersCount = await _context.FollowingBrands
                .CountAsync(f => f.BrandId == brand.Id);

            var productsCount = await _context.Products
                .CountAsync(p => p.BrandId == brand.Id && p.IsActive);

          

            
            var features = new List<string>
    {
        "Premium Quality",
        "Fast Shipping",
        "30-Day Returns",
        "Customer Support"
    };

            var brandDetails = new
            {
                BrandId = brand.Id,
                BrandName = brand.OfficialName,
                FollowersCount = followersCount,
                FollowersFormatted = FormatCount(followersCount),
                ProductsCount = productsCount,
                Location = brand.Location,
                Description = brand.Description,
                LogoUrl = brand.LogoUrl,
                Features = features,
                SocialMedia = new
                {
                    Facebook = brand.Facebook,
                    Instagram = brand.Instgram
                }
            };

            return Ok(brandDetails);
        }

        private string FormatCount(int count)
        {
            if (count >= 1000000)
                return $"{(count / 1000000.0):0.0}M";
            if (count >= 1000)
                return $"{(count / 1000.0):0.0}K";
            return count.ToString();
        }
        [HttpPost("product-feedback")]
        public async Task<IActionResult> AddFeedback([FromBody] AddProductFeedbackDto dto)
        {
            var userId = _userManager.GetUserId(User); 
            if (userId == null)
                return Unauthorized(new { message = "User not logged in" });

            var product = await _context.Products.FindAsync(dto.ProductId);
            if (product == null)
                return NotFound(new { message = "Product not found" });

            var feedback = new ProductFeedback
            {
                ProductId = dto.ProductId,
                UserId = userId,
                Rating = dto.Rating,
                Comment = dto.Comment
            };

            _context.ProductFeedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Feedback added successfully" });
        }

        [HttpGet("product-feedbacks")]
        public async Task<IActionResult> GetFeedbacks(
    [FromQuery] int productId,
    [FromQuery] int? stars, 
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 3)
        {
            if (page < 1) page = 1;

            var query = _context.ProductFeedbacks
                .Include(f => f.User)
                .Where(f => f.ProductId == productId)
                .AsQueryable();

            if (stars.HasValue)
            {
                query = query.Where(f => f.Rating == stars.Value);
            }

            var totalItems = await query.CountAsync();

            var feedbacks = await query
                .OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new ProductFeedbackDto
                {
                    Id = f.Id,
                    UserName = f.User != null ? f.User.FullName : "Anonymous",
                    Rating = f.Rating,
                    Comment = f.Comment,
                    CreatedAt = f.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                totalItems,
                totalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                feedbacks
            });
        }

    }
}
