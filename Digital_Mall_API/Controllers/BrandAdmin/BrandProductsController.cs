using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.DTOs.BrandAdminDTOs;
using Digital_Mall_API.Models.Entities.Product_Catalog;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Digital_Mall_API.Controllers.BrandAdmin
{
    [ApiController]
    [Route("Brand/Management/[controller]")]
    public class BrandProductsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public BrandProductsController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

       
        [HttpGet("All")]
        public async Task<ActionResult> GetAll([FromQuery] ProductQueryParameters parameters)
        {
            var query = _context.Products
                .Include(p => p.SubCategory).ThenInclude(sc => sc.Category)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .Include(p => p.Images)
                .AsQueryable();

            if (!string.IsNullOrEmpty(parameters.Search))
                query = query.Where(p => p.Name.Contains(parameters.Search));

            if (parameters.CategoryId.HasValue)
                query = query.Where(p => p.SubCategory.CategoryId == parameters.CategoryId.Value);

            if (parameters.SubCategoryId.HasValue)
                query = query.Where(p => p.SubCategoryId == parameters.SubCategoryId.Value);

            if (parameters.IsActive.HasValue)
                query = query.Where(p => p.IsActive == parameters.IsActive.Value);

            if (parameters.InStock.HasValue)
            {
                if (parameters.InStock.Value)
                    query = query.Where(p => p.Variants.Any(v => v.StockQuantity > 0));
                else
                    query = query.Where(p => p.Variants.All(v => v.StockQuantity == 0));
            }

            var totalCount = await query.CountAsync();

            var products = await query
                .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    IsActive = p.IsActive,
                    CategoryName = p.SubCategory.Category.Name,
                    SubCategoryName = p.SubCategory.Name,
                    Variants = p.Variants.Select(v => new ProductVariantDto
                    {
                        Id = v.Id,
                        Color = v.Color,
                        Size = v.Size,
                        Style = v.Style,
                        StockQuantity = v.StockQuantity,
                        SKU = v.SKU
                    }).ToList(),
                    Images = p.Images.Select(img => img.ImageUrl).ToList()
                }).ToListAsync();

            return Ok(new { Data = products, TotalCount = totalCount, parameters.PageNumber, parameters.PageSize });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDto>> GetById(int id)
        {
            var product = await _context.Products
                .Include(p => p.SubCategory).ThenInclude(sc => sc.Category)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                IsActive = product.IsActive,
                CategoryName = product.SubCategory.Category.Name,
                SubCategoryName = product.SubCategory.Name,
                Variants = product.Variants.Select(v => new ProductVariantDto
                {
                    Id = v.Id,
                    Color = v.Color,
                    Size = v.Size,
                    Style = v.Style,
                    StockQuantity = v.StockQuantity,
                    SKU = v.SKU
                }).ToList(),
                Images = product.Images.Select(img => img.ImageUrl).ToList()
            };
        }

     
        [HttpPost("Add")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult> Create([FromForm] ProductCreateUpdateDto dto, List<IFormFile> images)
        {
            var brandId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(brandId))
                return Unauthorized("Brand identifier not found in token");

            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                IsActive = dto.IsActive,
                BrandId = brandId,
                SubCategoryId = dto.SubCategoryId,
                Variants = dto.Variants.Select(v => new ProductVariant
                {
                    Color = v.Color,
                    Size = v.Size,
                    Style = v.Style,
                    StockQuantity = v.StockQuantity,
                    SKU = v.SKU
                }).ToList()
            };

            
            if (images != null && images.Count > 0)
            {
                foreach (var file in images)
                {
                    var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                    var path = Path.Combine(_env.WebRootPath, "uploads", "products", fileName);

                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    using var stream = new FileStream(path, FileMode.Create);
                    await file.CopyToAsync(stream);

                    product.Images.Add(new ProductImage { ImageUrl = $"/uploads/products/{fileName}" });
                }
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return Ok(new { product.Id, product.Name });
        }

      
        [HttpPut("Update/{id}")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult> Update(int id, [FromForm] ProductCreateUpdateDto dto, List<IFormFile> images)
        {
            var product = await _context.Products
                .Include(p => p.Variants)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.IsActive = dto.IsActive;
            product.SubCategoryId = dto.SubCategoryId;

            _context.ProductVariants.RemoveRange(product.Variants);
            product.Variants = dto.Variants.Select(v => new ProductVariant
            {
                Color = v.Color,
                Size = v.Size,
                Style = v.Style,
                StockQuantity = v.StockQuantity,
                SKU = v.SKU
            }).ToList();

            if (images != null && images.Count > 0)
            {
                _context.ProductImages.RemoveRange(product.Images);

                foreach (var file in images)
                {
                    var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                    var path = Path.Combine(_env.WebRootPath, "uploads", "products", fileName);

                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    using var stream = new FileStream(path, FileMode.Create);
                    await file.CopyToAsync(stream);

                    product.Images.Add(new ProductImage { ImageUrl = $"/uploads/products/{fileName}" });
                }
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

      
        [HttpDelete("Delete/{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var product = await _context.Products
                .Include(p => p.Variants)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            _context.ProductVariants.RemoveRange(product.Variants);
            _context.ProductImages.RemoveRange(product.Images);
            _context.Products.Remove(product);

            await _context.SaveChangesAsync();

            return Ok();
        }
    }


}
