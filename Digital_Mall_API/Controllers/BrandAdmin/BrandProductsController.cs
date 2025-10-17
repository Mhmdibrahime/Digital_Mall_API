using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.DTOs.BrandAdminDTOs;
using Digital_Mall_API.Models.Entities.Product_Catalog;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;

namespace Digital_Mall_API.Controllers.BrandAdmin
{
    [ApiController]
    [Route("Brand/Management/[controller]")]
    public class BrandProductsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<BrandProductsController> _logger;

        public BrandProductsController(AppDbContext context, IWebHostEnvironment env, ILogger<BrandProductsController> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
        }


        [HttpGet("All")]
        public async Task<ActionResult> GetAll([FromQuery] ProductQueryParameters parameters)
        {
            var brandId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(brandId))
                return Unauthorized("Brand not authorized");
            var brand = await _context.Brands.FindAsync(brandId);
            if(brand == null)
                return NotFound("Brand not found"); 
            var query = _context.Products
                .Where(p => p.BrandId == brandId)
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
                    Gender = p.Gender,
                    Variants = p.Variants.Select(v => new ProductVariantDto
                    {
                        Id = v.Id,
                        Color = v.Color,
                        Size = v.Size,
                        StockQuantity = v.StockQuantity,
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
                Gender = product.Gender,
                Variants = product.Variants.Select(v => new ProductVariantDto
                {
                    Id = v.Id,
                    Color = v.Color,
                    Size = v.Size,
                    StockQuantity = v.StockQuantity,
                }).ToList(),
                Images = product.Images.Select(img => img.ImageUrl).ToList()
            };
        }

        [HttpGet("GetCategories")]
        public async Task<ActionResult> GetCategories()
        {
            var categories = await _context.Categories
                .Select(c => new
                {
                    Id = c.Id,
                    Name = c.Name
                })
                .ToListAsync();
            return Ok(categories);
        }

        [HttpGet("GetSubcategoriesByCategory/{categoryId}")]
        public async Task<ActionResult> GetSubcategoriesByCategory(int categoryId)
        {
            var subcategories = await _context.SubCategories
                .Where(sc => sc.CategoryId == categoryId)
                .Select(sc => new
                {
                    Id = sc.Id,
                    Name = sc.Name
                })
                .ToListAsync();

            return Ok(subcategories);
        }

        [HttpPost("Add")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult> Create(
    [FromForm] ProductCreateDto dto,
    [FromQuery] string pVariantsJson 
)
        {
            
            var brandId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(brandId))
                return Unauthorized("Brand identifier not found in token");

            try
            {

                var PVariants = new List<VariantCreateDto>();
                if (!string.IsNullOrEmpty(pVariantsJson))
                {
                    PVariants = JsonConvert.DeserializeObject<List<VariantCreateDto>>(pVariantsJson);
                }

                var product = new Product
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    Price = dto.Price,
                    Gender = dto.Gender,
                    IsActive = dto.IsActive,
                    BrandId = brandId,
                    SubCategoryId = dto.SubCategoryId
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                if (PVariants != null && PVariants.Any())
                {
                    foreach (var variantDto in PVariants)
                    {
                        _context.ProductVariants.Add(new ProductVariant
                        {
                            ProductId = product.Id,
                            Color = variantDto.Color,
                            Size = variantDto.Size,
                            StockQuantity = variantDto.StockQuantity
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                if (dto.Images != null && dto.Images.Count > 0)
                {
                    foreach (var file in dto.Images)
                    {
                        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                        var path = Path.Combine(_env.WebRootPath, "uploads", "products", fileName);

                        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                        using var stream = new FileStream(path, FileMode.Create);
                        await file.CopyToAsync(stream);

                        _context.ProductImages.Add(new ProductImage
                        {
                            ProductId = product.Id,
                            ImageUrl = $"/uploads/products/{fileName}"
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                return Ok(new { product.Id, product.Name });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product");
                return BadRequest("An error occurred while adding the product");
            }
        }



        [HttpPut("Update/{id}")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult> Update(
    int id,
    [FromForm] ProductCreateDto dto,
    [FromQuery] string pVariantsJson)
        {
            var product = await _context.Products
                .Include(p => p.Variants)
                    .ThenInclude(v => v.OrderItems) // Include OrderItems to check for associations
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound("Product not found");

            try
            {
                // Deserialize variants from JSON query parameter
                var PVariants = new List<VariantCreateDto>();
                if (!string.IsNullOrEmpty(pVariantsJson))
                {
                    PVariants = JsonConvert.DeserializeObject<List<VariantCreateDto>>(pVariantsJson);
                }

                // Update product basic fields
                product.Name = dto.Name;
                product.Description = dto.Description;
                product.Price = dto.Price;
                product.IsActive = dto.IsActive;
                product.Gender = dto.Gender;
                product.SubCategoryId = dto.SubCategoryId;

                // Smart variant update logic
                if (PVariants != null && PVariants.Any())
                {
                    await UpdateProductVariants(product, PVariants);
                }
                else
                {
                    // If no variants provided, remove only variants without orders
                    var variantsWithoutOrders = product.Variants
                        .Where(v => !v.OrderItems.Any())
                        .ToList();

                    _context.ProductVariants.RemoveRange(variantsWithoutOrders);
                }

                // Handle product images (images typically don't have order associations)
                if (dto.Images != null && dto.Images.Count > 0)
                {
                    // Remove old images (usually safe since images don't have direct order relationships)
                    _context.ProductImages.RemoveRange(product.Images);

                    foreach (var file in dto.Images)
                    {
                        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                        var path = Path.Combine(_env.WebRootPath, "uploads", "products", fileName);

                        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                        using var stream = new FileStream(path, FileMode.Create);
                        await file.CopyToAsync(stream);

                        _context.ProductImages.Add(new ProductImage
                        {
                            ProductId = product.Id,
                            ImageUrl = $"/uploads/products/{fileName}"
                        });
                    }
                }

                await _context.SaveChangesAsync();
                return Ok(new { Message = "Product updated successfully", product.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product");
                return BadRequest("An error occurred while updating the product");
            }
        }

        private async Task UpdateProductVariants(Product product, List<VariantCreateDto> newVariants)
        {
            var existingVariants = product.Variants.ToList();
            var variantsWithOrders = new List<ProductVariant>();
            var variantsToRemove = new List<ProductVariant>();
            var variantsToUpdate = new List<ProductVariant>();
            var variantsToAdd = new List<ProductVariant>();

            // Separate variants with and without orders
            foreach (var existingVariant in existingVariants)
            {
                if (existingVariant.OrderItems.Any())
                {
                    variantsWithOrders.Add(existingVariant);
                }
                else
                {
                    variantsToRemove.Add(existingVariant);
                }
            }

            // Match existing variants with new variant data
            foreach (var newVariant in newVariants)
            {
                // Try to find existing variant with same color/size
                var existingVariant = existingVariants
                    .FirstOrDefault(v =>
                        v.Color == newVariant.Color &&
                        v.Size == newVariant.Size);

                if (existingVariant != null)
                {
                    // Update existing variant
                    existingVariant.StockQuantity = newVariant.StockQuantity;
                    variantsToUpdate.Add(existingVariant);

                    // Remove from removal list if it was there
                    variantsToRemove.Remove(existingVariant);
                }
                else
                {
                    // Create new variant
                    variantsToAdd.Add(new ProductVariant
                    {
                        ProductId = product.Id,
                        Color = newVariant.Color,
                        Size = newVariant.Size,
                        StockQuantity = newVariant.StockQuantity
                    });
                }
            }

            // Remove only variants that don't have orders and aren't being updated
            var finalVariantsToRemove = variantsToRemove
                .Where(v => !variantsToUpdate.Contains(v))
                .ToList();

            _context.ProductVariants.RemoveRange(finalVariantsToRemove);
            _context.ProductVariants.AddRange(variantsToAdd);

            // Log warning for variants with orders that couldn't be modified
            var preservedVariantsCount = variantsWithOrders.Count;
            if (preservedVariantsCount > 0)
            {
                _logger.LogWarning($"Preserved {preservedVariantsCount} variants with existing orders for product {product.Id}");
            }
        }


        [HttpDelete("Delete/{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var product = await _context.Products
                .Include(p => p.Variants)
                    .ThenInclude(v => v.OrderItems)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            var variantsWithOrders = product.Variants
                .Where(v => v.OrderItems.Any())
                .ToList();

            if (variantsWithOrders.Any())
            {
                var totalOrders = variantsWithOrders.Sum(v => v.OrderItems.Count);

                
                product.IsActive = false;

               


                await _context.SaveChangesAsync();

                return BadRequest($"Product deactivated due to {totalOrders} existing orders associated with it");
            }

            _context.ProductVariants.RemoveRange(product.Variants);
            _context.ProductImages.RemoveRange(product.Images);
            _context.Products.Remove(product);

            await _context.SaveChangesAsync();

            return Ok("Product deleted successfully");
        }


    }


}
