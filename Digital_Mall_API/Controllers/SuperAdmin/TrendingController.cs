using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Digital_Mall_API.Models.Entities.Product_Catalog;
using Digital_Mall_API.Models.DTOs;
using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.DTOs.SuperAdminDTOs.TrendingDTOs;

namespace Digital_Mall_API.Controllers.SuperAdmin
{
    [Route("Super/[controller]")]
    [ApiController]
    public class TrendingController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TrendingController(AppDbContext context)
        {
            _context = context;
        }

       

        [HttpGet("products")]
        public async Task<ActionResult<IEnumerable<ProductResponseDto>>> GetProducts(
            [FromQuery] string search = null,
            [FromQuery] string brand = null
          )
        {
            var query = _context.Products
                .Include(p => p.Brand)
                .Include(p => p.SubCategory)
                .ThenInclude(sc => sc.Category)
                .Include(p => p.Images)
                .Where(p => p.IsActive);

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.Contains(search) || p.Id.ToString().Contains(search));
            }

            if (!string.IsNullOrEmpty(brand) && brand != "All Brands")
            {
                query = query.Where(p => p.Brand.OfficialName == brand);
            }


            var products = await query
                .OrderByDescending(p => p.IsTrend)
                .ThenBy(p => p.Name)
                .Select(p => new ProductResponseDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Category = p.SubCategory.Name,
                    ProductId = $"prod_{p.Id:D3}",
                    Brand = p.Brand.OfficialName,
                    Status = p.IsTrend ? "Trending" : "Regular",
                    Price = p.Price,
                    IsTrend = p.IsTrend,
                    Images = p.Images
                       
                        .OrderBy(img => img.DisplayOrder)
                        .Select(img => new ProductImageDto
                        {
                            Id = img.Id,
                            ImageUrl = img.ImageUrl,
                            DisplayOrder = img.DisplayOrder
                        })
                        .ToList()
                })
                .ToListAsync();

            return Ok(products);
        }

        [HttpGet("product/{id}")]
        public async Task<ActionResult<ProductResponseDto>> GetProductById(int id)
        {
            var product = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.SubCategory)
                .ThenInclude(sc => sc.Category)
                .Include(p => p.Images)
                .Where(p => p.IsActive && p.Id == id)
                .Select(p => new ProductResponseDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Category = p.SubCategory.Name,
                    ProductId = $"prod_{p.Id:D3}",
                    Brand = p.Brand.OfficialName,
                    Status = p.IsTrend ? "Trending" : "Regular",
                    Price = p.Price,
                    IsTrend = p.IsTrend,
                    Images = p.Images
                        .OrderBy(img => img.DisplayOrder)
                        .Select(img => new ProductImageDto
                        {
                            Id = img.Id,
                            ImageUrl = img.ImageUrl,
                            DisplayOrder = img.DisplayOrder
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (product == null)
            {
                return NotFound(new { message = "Product not found" });
            }

            return Ok(product);
        }

        [HttpGet("brands")]
        public async Task<ActionResult<IEnumerable<BrandResponseDto>>> GetBrands()
        {
            var brands = await _context.Brands
                .Where(b => b.Status == "Active")
                .OrderBy(b => b.OfficialName)
                .Select(b => new BrandResponseDto
                {
                    Id = b.Id,
                    Name = b.OfficialName
                })
                .ToListAsync();

            brands.Insert(0, new BrandResponseDto { Id = "all", Name = "All Brands" });

            return Ok(brands);
        }

       
        [HttpPost("{productId}/change-trending-status")]
        public async Task<ActionResult> ToggleTrendingStatus(int productId)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                return NotFound(new { message = "Product not found" });
            }

            if (!product.IsActive)
            {
                return BadRequest(new { message = "Cannot modify trending status for inactive product" });
            }

            product.IsTrend = !product.IsTrend;
            await _context.SaveChangesAsync();

            var productResponse = new ProductResponseDto
            {
                Id = product.Id,
                Name = product.Name,
                Category = product.SubCategory?.Name ?? "N/A",
                ProductId = $"prod_{product.Id:D3}",
                Brand = product.Brand?.OfficialName ?? "N/A",
                Status = product.IsTrend ? "Trending" : "Regular",
                Price = product.Price,
                IsTrend = product.IsTrend,
                Images = product.Images
                    .OrderBy(img => img.DisplayOrder)
                    .Select(img => new ProductImageDto
                    {
                        Id = img.Id,
                        ImageUrl = img.ImageUrl,
                        DisplayOrder = img.DisplayOrder
                    })
                    .ToList()
            };

            var action = product.IsTrend ? "marked as trending" : "removed from trending";
            return Ok(new
            {
                message = $"Product {action} successfully",
                isTrend = product.IsTrend,
                product = productResponse
            });
        }

       

        [HttpGet("trending-count")]
        public async Task<ActionResult<int>> GetTrendingProductsCount()
        {
            var count = await _context.Products
                .Where(p => p.IsActive && p.IsTrend)
                .CountAsync();

            return Ok(count);
        }

        [HttpGet("product-images/{productId}")]
        public async Task<ActionResult<List<ProductImageDto>>> GetProductImages(int productId)
        {
            var images = await _context.ProductImages
                .Where(img => img.ProductId == productId )
                .OrderBy(img => img.DisplayOrder)
                .Select(img => new ProductImageDto
                {
                    Id = img.Id,
                    ImageUrl = img.ImageUrl,
                    DisplayOrder = img.DisplayOrder
                })
                .ToListAsync();

            if (!images.Any())
            {
                return NotFound(new { message = "No images found for this product" });
            }

            return Ok(images);
        }
    }

   
}