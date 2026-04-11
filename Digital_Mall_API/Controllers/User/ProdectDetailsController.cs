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
                .Include(p => p.SubSubCategory)
        .ThenInclude(ssc => ssc.SubCategory)
            .ThenInclude(sc => sc.Category)
                .Include(p => p.ProductDiscount)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.Images)          // ← include variant images
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (product == null)
                return NotFound(new { message = "Product not found" });

            // Available colors (grouped) – kept for backward compatibility
            var availableColors = product.Variants
                .Where(v => v.StockQuantity > 0)
                .GroupBy(v => v.Color)
                .Select(g => new
                {
                    Color = g.Key,
                    AvailableStock = g.Sum(v => v.StockQuantity),
                    HasMultipleSizes = g.Select(v => v.Size).Distinct().Count() > 1
                })
                .ToList();

            // Full variant details including images
            var variants = product.Variants.Select(v => new
            {
                v.Id,
                v.Color,
                v.Size,
                v.StockQuantity,
                v.ColorName,
                v.Price,
                Images = v.Images.Select(img => img.ImageUrl).ToList()
            }).ToList();

            var images = product.Images.Select(i => i.ImageUrl).ToList();

            var feedbacksCount = await _context.ProductFeedbacks.CountAsync(f => f.ProductId == id);
            var averageRating = await _context.ProductFeedbacks
                .Where(f => f.ProductId == id)
                .AverageAsync(f => (double?)f.Rating) ?? 0;

            decimal originalPrice = product.Price;
            decimal discountValue = product.ProductDiscount?.DiscountValue ?? 0;
            string discountStatus = discountValue > 0 ? "Active" : "None";
            decimal discountedPrice = product.ProductDiscount != null
                ? product.Price - (product.Price * product.ProductDiscount.DiscountValue / 100)
                : product.Price;

            var productDetails = new
            {
                product.Id,
                product.Name,
                Description = product.Description,
                BrandName = product.Brand?.OfficialName,
                Category = product.SubSubCategory.SubCategory?.Category?.EnglishName,
                CategoryInArabic = product.SubSubCategory.SubCategory?.Category?.ArabicName,
                SubCategory = product.SubSubCategory.SubCategory?.EnglishName,
                SubCategoryInArabic = product.SubSubCategory.SubCategory?.ArabicName,
                SubSubCategoryInArabic=product.SubSubCategory.ArabicName,
                SubSubCategoryInEnglish=product.SubSubCategory.EnglishName,
                OriginalPrice = originalPrice,
                DiscountValue = discountValue,
                DiscountedPrice = discountedPrice,
                DiscountStatus = discountStatus,
                AvailableColors = availableColors,
                Variants = variants,                    
                TotalStockQuantity = product.Variants.Sum(v => v.StockQuantity),
                Images = images,
                FeedBacksCount = feedbacksCount,
                AverageRating = Math.Round(averageRating, 1),
                CreatedAt = product.CreatedAt
            };

            return Ok(productDetails);
        }
        [HttpGet("product-variants/{productId}/color/{color}")]
        public async Task<IActionResult> GetSizesForColor(int productId, string color)
        {
            try
            {
                var variants = await _context.ProductVariants
                    .Include(v => v.Images)                     
                    .Include(v => v.Product)                    
                        .ThenInclude(p => p.ProductDiscount)    
                    .Where(v => v.ProductId == productId &&
                               v.Color.ToLower() == color.ToLower() &&
                               v.StockQuantity > 0)
                    .Select(v => new
                    {
                        VariantId = v.Id,
                        Size = v.Size,
                        StockQuantity = v.StockQuantity,
                        Images = v.Images.Select(img => img.ImageUrl).ToList(),
                        FinalPrice = v.Product.ProductDiscount != null
                            ? v.Product.Price - (v.Product.Price * v.Product.ProductDiscount.DiscountValue / 100)
                            : v.Product.Price
                    })
                    .OrderBy(v => v.Size)
                    .ToListAsync();

                if (!variants.Any())
                {
                    return NotFound(new { message = "No available variants found for this color" });
                }

                return Ok(new
                {
                    Color = color,
                    AvailableSizes = variants,
                    TotalAvailable = variants.Sum(v => v.StockQuantity)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving variant information", error = ex.Message });
            }
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
                IsCertified = brand.IsCertified,
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
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> AddFeedback([FromForm] AddProductFeedbackDto dto)
        {
            var userId = _userManager.GetUserId(User); 
            if (userId == null)
                return Unauthorized(new { message = "User not logged in" });

            var product = await _context.Products.FindAsync(dto.ProductId);
            if (product == null)
                return NotFound(new { message = "Product not found" });

            try
            {
                string fileUrl = string.Empty;
                if (dto.File != null)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "feedbacks");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    var uniqueFileName = $"{Guid.NewGuid()}_{dto.File.FileName}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await dto.File.CopyToAsync(fileStream);
                    }
                    fileUrl = $"/uploads/feedbacks/{uniqueFileName}";

                }
                var feedbackWithImage = new ProductFeedback
                {
                    ProductId = dto.ProductId,
                    UserId = userId,
                    Rating = dto.Rating,
                    Comment = dto.Comment,
                    ImageUrl = fileUrl
                };
                _context.ProductFeedbacks.Add(feedbackWithImage);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Feedback added successfully" });
            }
            catch(Exception ex)
            {
                return StatusCode(500, $"Error while adding the feedback: {ex.Message}");
            }
            //var feedback = new ProductFeedback
            //{
            //    ProductId = dto.ProductId,
            //    UserId = userId,
            //    Rating = dto.Rating,
            //    Comment = dto.Comment
            //};

            //_context.ProductFeedbacks.Add(feedback);
            //await _context.SaveChangesAsync();

            // return Ok(new { message = "Feedback added successfully" });
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
                    ImageUrl = f.ImageUrl,
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
