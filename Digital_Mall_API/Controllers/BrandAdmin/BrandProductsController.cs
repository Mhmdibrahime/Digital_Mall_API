using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.DTOs.BrandAdminDTOs;
using Digital_Mall_API.Models.Entities.Product_Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;

namespace Digital_Mall_API.Controllers.BrandAdmin
{
    [ApiController]
    [Route("Brand/Management/[controller]")]
    //[Authorize(Roles = "Brand")]

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
            if (brand == null)
                return NotFound("Brand not found");

            var query = _context.Products
                .Where(p => p.BrandId == brandId)
                .Include(p => p.SubSubCategory)
        .ThenInclude(ssc => ssc.SubCategory)
            .ThenInclude(sc => sc.Category)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.Images)
                .Include(p => p.Images)
                .AsQueryable();

            if (!string.IsNullOrEmpty(parameters.Search))
                query = query.Where(p => p.Name.Contains(parameters.Search));

            if (parameters.CategoryId.HasValue)
                query = query.Where(p => p.SubSubCategory.SubCategory.CategoryId == parameters.CategoryId.Value);

            if (parameters.SubCategoryId.HasValue)
                query = query.Where(p => p.SubSubCategory.SubCategoryId == parameters.SubCategoryId.Value);

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
                    CategoryName = p.SubSubCategory.SubCategory.Category.EnglishName,
                    SubCategoryName = p.SubSubCategory.SubCategory.EnglishName,
                    SubSubCategoryName= p.SubSubCategory.EnglishName,
                    Gender = p.Gender,
                    Variants = p.Variants.Select(v => new ProductVariantDto
                    {
                        Id = v.Id,
                        Color = v.Color,
                        ColorName = v.ColorName,
                        Size = v.Size,
                        StockQuantity = v.StockQuantity,
                        Price = v.Price,
                        Images = v.Images.Select(img => img.ImageUrl).ToList()
                    }).ToList(),
                    Images = p.Images.Select(img => img.ImageUrl).ToList()
                }).ToListAsync();

            return Ok(new { Data = products, TotalCount = totalCount, parameters.PageNumber, parameters.PageSize });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDto>> GetById(int id)
        {
            var product = await _context.Products
                .Include(p => p.SubSubCategory)
        .ThenInclude(ssc => ssc.SubCategory)
            .ThenInclude(sc => sc.Category)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.Images)          // ← include variant images
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                IsActive = product.IsActive,
                CategoryName = product.SubSubCategory.SubCategory.Category.EnglishName,
                SubCategoryName = product.SubSubCategory.SubCategory.EnglishName,
                SubSubCategoryName = product.SubSubCategory.EnglishName,
                Gender = product.Gender,
                Variants = product.Variants.Select(v => new ProductVariantDto
                {
                    Id = v.Id,
                    Color = v.Color,
                    ColorName = v.ColorName,
                    Size = v.Size,
                    StockQuantity = v.StockQuantity,
                    Price = v.Price,
                    Images = v.Images.Select(img => img.ImageUrl).ToList()
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
                    Name = c.EnglishName
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
                    Name = sc.EnglishName
                })
                .ToListAsync();

            return Ok(subcategories);
        }

        [HttpPost("AddFlat")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult> CreateFlat([FromForm] ProductCreateFlatDto dto)
        {
            var brandId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(brandId))
                return Unauthorized("Brand identifier not found in token");

            try
            {
                // عدد المتغيرات الأساسي
                int variantCount = dto.VariantColors.Count;

                // التحقق من تطابق أطوال القوائم الإلزامية
                if (variantCount != dto.VariantSizes.Count ||
                    variantCount != dto.VariantStockQuantities.Count)
                {
                    return BadRequest("Variant data lists (Colors, Sizes, StockQuantities) must have the same length.");
                }

                // التحقق من أطوال القوائم الاختيارية إذا كانت موجودة
                if (dto.VariantColorNames != null && dto.VariantColorNames.Count != variantCount)
                    return BadRequest("VariantColorNames list must have the same length as VariantColors.");
                if (dto.VariantPrices != null && dto.VariantPrices.Count != variantCount)
                    return BadRequest("VariantPrices list must have the same length as VariantColors.");

                // التحقق من أن كل مؤشر صورة موجود ضمن النطاق
                if (dto.VariantImageIndices.Any(idx => idx < 0 || idx >= variantCount))
                    return BadRequest("Variant image index out of range.");

                await using var transaction = await _context.Database.BeginTransactionAsync();

                // 1. إنشاء المنتج
                var product = new Product
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    Price = dto.Price,
                    Gender = dto.Gender,
                    IsActive = dto.IsActive,
                    BrandId = brandId,
                    SubSubCategoryId = dto.SubSubCategoryId  
                };
                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                // 2. حفظ صور المنتج
                if (dto.Images != null)
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

                // 3. إنشاء المتغيرات مع دعم ColorName و Price
                var variantIds = new List<int>();
                for (int i = 0; i < variantCount; i++)
                {
                    var variant = new ProductVariant
                    {
                        ProductId = product.Id,
                        Color = dto.VariantColors[i],
                        ColorName = dto.VariantColorNames?[i],           // قد تكون null
                        Size = dto.VariantSizes[i],
                        StockQuantity = dto.VariantStockQuantities[i],
                        Price = dto.VariantPrices?[i]                    // قد تكون null
                    };
                    _context.ProductVariants.Add(variant);
                    await _context.SaveChangesAsync(); // للحصول على ID
                    variantIds.Add(variant.Id);
                }

                // 4. ربط الصور بالمتغيرات باستخدام الـ indices
                for (int fileIdx = 0; fileIdx < dto.VariantImageFiles.Count; fileIdx++)
                {
                    var file = dto.VariantImageFiles[fileIdx];
                    var variantIndex = dto.VariantImageIndices[fileIdx];
                    var variantId = variantIds[variantIndex];

                    var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                    var path = Path.Combine(_env.WebRootPath, "uploads", "variantimages", fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    using var stream = new FileStream(path, FileMode.Create);
                    await file.CopyToAsync(stream);

                    _context.ProductVariantImages.Add(new ProductVariantImage
                    {
                        ProductVariantId = variantId,
                        ImageUrl = $"/uploads/variantimages/{fileName}"
                    });
                }
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return Ok(new { product.Id, product.Name });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product with flat DTO");
                return BadRequest("An error occurred while adding the product");
            }
        }

        [HttpPut("UpdateFlat/{id}")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult> UpdateFlat(int id, [FromForm] ProductUpdateDto dto)
        {
            var brandId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(brandId))
                return Unauthorized("Brand identifier not found in token");

            var product = await _context.Products
                .Include(p => p.Variants)
                    .ThenInclude(v => v.OrderItems)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.Images)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id && p.BrandId == brandId);

            if (product == null)
                return NotFound("Product not found");

            // Validate that variant lists have the same length
            if (dto.VariantColors.Count != dto.VariantSizes.Count ||
                dto.VariantColors.Count != dto.VariantStockQuantities.Count)
            {
                return BadRequest("Variant data lists must have the same length.");
            }

            // Validate that each image index is within range
            if (dto.VariantImageIndices.Any(idx => idx < 0 || idx >= dto.VariantColors.Count))
            {
                return BadRequest("Variant image index out of range.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Log incoming variant data
                _logger.LogInformation("Updating product {ProductId} with {VariantCount} variants", id, dto.VariantColors.Count);
                for (int i = 0; i < dto.VariantColors.Count; i++)
                {
                    _logger.LogInformation($"Variant {i}: Color={dto.VariantColors[i]}, Size={dto.VariantSizes[i]}, Stock={dto.VariantStockQuantities[i]}, " +
                $"ColorName={(dto.VariantColorNames?[i] ?? "null")}, Price={(dto.VariantPrices?[i]?.ToString() ?? "null")}");
                }

                product.Name = dto.Name;
                product.Description = dto.Description;
                product.Price = dto.Price;
                product.IsActive = dto.IsActive;
                product.Gender = dto.Gender;
                product.SubSubCategoryId = dto.SubSubCategoryId;

                // ---------- Product images ----------
                if (dto.ImagesToDelete?.Any() == true)
                {
                    var imagesToDelete = product.Images
                        .Where(img => dto.ImagesToDelete.Contains(img.ImageUrl))
                        .ToList();
                    foreach (var img in imagesToDelete)
                    {
                        var physicalPath = Path.Combine(_env.WebRootPath, img.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(physicalPath))
                            System.IO.File.Delete(physicalPath);
                        _context.ProductImages.Remove(img);
                    }
                    _logger.LogInformation($"Deleted {imagesToDelete.Count} product images");
                }

                if (dto.NewImages?.Any() == true)
                {
                    const int maxImages = 10;
                    var currentImageCount = product.Images.Count - (dto.ImagesToDelete?.Count ?? 0);
                    if (currentImageCount + dto.NewImages.Count > maxImages)
                        return BadRequest($"Cannot upload {dto.NewImages.Count} new images. Maximum {maxImages} allowed.");

                    foreach (var file in dto.NewImages)
                    {
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                        if (!allowedExtensions.Contains(extension))
                            return BadRequest($"Invalid file type: {extension}");

                        const long maxFileSize = 5 * 1024 * 1024;
                        if (file.Length > maxFileSize)
                            return BadRequest($"File {file.FileName} exceeds 5MB.");

                        var fileName = $"{Guid.NewGuid()}{extension}";
                        var uploadPath = Path.Combine("uploads", "products", fileName);
                        var fullPath = Path.Combine(_env.WebRootPath, uploadPath);

                        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                        using var stream = new FileStream(fullPath, FileMode.Create);
                        await file.CopyToAsync(stream);

                        _context.ProductImages.Add(new ProductImage
                        {
                            ProductId = product.Id,
                            ImageUrl = $"/{uploadPath.Replace("\\", "/")}"
                        });
                    }
                    _logger.LogInformation($"Added {dto.NewImages.Count} new product images");
                }

                // ---------- Variant image deletions ----------
                if (dto.VariantImageIdsToDelete?.Any() == true)
                {
                    var variantImagesToDelete = await _context.ProductVariantImages
                        .Where(vi => dto.VariantImageIdsToDelete.Contains(vi.Id))
                        .ToListAsync();

                    foreach (var vi in variantImagesToDelete)
                    {
                        var physicalPath = Path.Combine(_env.WebRootPath, vi.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(physicalPath))
                            System.IO.File.Delete(physicalPath);
                        _context.ProductVariantImages.Remove(vi);
                    }
                    _logger.LogInformation($"Deleted {variantImagesToDelete.Count} variant images by ID");
                }

                // ---------- Variant updates ----------
                var existingVariants = product.Variants.ToDictionary(v => (v.Color, v.Size));
                var matchedExistingVariantIds = new HashSet<int>();
                var newVariantsList = new List<ProductVariant>();
                var variantIdsInOrder = new List<int>();

                for (int i = 0; i < dto.VariantColors.Count; i++)
                {
                    var color = dto.VariantColors[i];
                    var size = dto.VariantSizes[i];
                    var stock = dto.VariantStockQuantities[i];
                    var colorName = dto.VariantColorNames?[i];   // قد تكون null
                    var price = dto.VariantPrices?[i];           // قد تكون null
                    var key = (color, size);

                    if (existingVariants.TryGetValue(key, out var existingVariant))
                    {
                        // Update existing variant
                        existingVariant.StockQuantity = stock;
                        if (colorName != null)
                            existingVariant.ColorName = colorName;   // تحديث اسم اللون إذا أرسل
                        if (price.HasValue)
                            existingVariant.Price = price;           // تحديث السعر إذا أرسل
                        variantIdsInOrder.Add(existingVariant.Id);
                        matchedExistingVariantIds.Add(existingVariant.Id);
                        _logger.LogInformation($"Matched existing variant ID {existingVariant.Id} for {color}/{size}");
                    }
                    else
                    {
                        // Create new variant
                        var newVariant = new ProductVariant
                        {
                            ProductId = product.Id,
                            Color = color,
                            ColorName = colorName,
                            Size = size,
                            StockQuantity = stock,
                            Price = price
                        };
                        newVariantsList.Add(newVariant);
                        variantIdsInOrder.Add(-1);
                        _logger.LogInformation($"New variant to create for {color}/{size}");
                    }
                }

                // Save new variants and add their IDs to matched set
                if (newVariantsList.Any())
                {
                    _context.ProductVariants.AddRange(newVariantsList);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Saved {newVariantsList.Count} new variants.");

                    int newIdx = 0;
                    for (int i = 0; i < variantIdsInOrder.Count; i++)
                    {
                        if (variantIdsInOrder[i] == -1)
                        {
                            variantIdsInOrder[i] = newVariantsList[newIdx].Id;
                            matchedExistingVariantIds.Add(newVariantsList[newIdx].Id);
                            _logger.LogInformation($"New variant ID {newVariantsList[newIdx].Id} assigned to index {i}");
                            newIdx++;
                        }
                    }
                }

                // Remove variants that are no longer present and have no orders
                var variantsToRemove = product.Variants
                    .Where(v => !matchedExistingVariantIds.Contains(v.Id))
                    .ToList();

                _logger.LogInformation($"Variants to remove: {variantsToRemove.Count}");
                foreach (var variant in variantsToRemove)
                {
                    if (!variant.OrderItems.Any())
                    {
                        // Delete associated images physically
                        if (variant.Images != null)
                        {
                            foreach (var img in variant.Images)
                            {
                                var physPath = Path.Combine(_env.WebRootPath, img.ImageUrl.TrimStart('/'));
                                if (System.IO.File.Exists(physPath))
                                    System.IO.File.Delete(physPath);
                            }
                        }
                        _context.ProductVariants.Remove(variant);
                        _logger.LogInformation($"Removed variant ID {variant.Id}");
                    }
                    else
                    {
                        _logger.LogWarning($"Variant ID {variant.Id} with orders cannot be removed.");
                    }
                }

                // ---------- New variant images ----------
                if (dto.VariantImageFiles?.Any() == true)
                {
                    _logger.LogInformation($"Processing {dto.VariantImageFiles.Count} variant image files");
                    for (int fileIdx = 0; fileIdx < dto.VariantImageFiles.Count; fileIdx++)
                    {
                        var file = dto.VariantImageFiles[fileIdx];
                        var variantIndex = dto.VariantImageIndices[fileIdx];
                        var variantId = variantIdsInOrder[variantIndex];

                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                        if (!allowedExtensions.Contains(extension))
                            return BadRequest($"Invalid file type for variant image: {extension}");

                        const long maxFileSize = 5 * 1024 * 1024;
                        if (file.Length > maxFileSize)
                            return BadRequest($"Variant image file {file.FileName} exceeds 5MB.");

                        var fileName = $"{Guid.NewGuid()}{extension}";
                        var uploadPath = Path.Combine("uploads", "variantimages", fileName);
                        var fullPath = Path.Combine(_env.WebRootPath, uploadPath);

                        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                        using var stream = new FileStream(fullPath, FileMode.Create);
                        await file.CopyToAsync(stream);

                        _context.ProductVariantImages.Add(new ProductVariantImage
                        {
                            ProductVariantId = variantId,
                            ImageUrl = $"/{uploadPath.Replace("\\", "/")}"
                        });
                        _logger.LogInformation($"Added image {fileName} for variant ID {variantId}");
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                _logger.LogInformation("Product updated successfully");

                return Ok(new { Message = "Product updated successfully", product.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating product with flat DTO");
                return BadRequest("An error occurred while updating the product");
            }
        }

        [HttpDelete("Delete/{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var product = await _context.Products
                .Include(p => p.Variants)
                    .ThenInclude(v => v.OrderItems)
                .Include(p => p.Images)
                .Include(p => p.ReelProducts) // Include ReelProducts
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            // Check if product has any orders through its variants
            var hasOrders = product.Variants?.Any(v => v.OrderItems?.Any() == true) == true;

            // Check if product has any reels
            var hasReels = product.ReelProducts?.Any() == true;

            // If product has orders OR reels, deactivate it instead of deleting
            if (hasOrders || hasReels)
            {
                var messageParts = new List<string>();

                if (hasOrders)
                {
                    var totalOrders = product.Variants.Sum(v => v.OrderItems?.Count ?? 0);
                    messageParts.Add($"{totalOrders} orders");
                }

                if (hasReels)
                {
                    var reelCount = product.ReelProducts.Count;
                    messageParts.Add($"{reelCount} reels");
                }

                product.IsActive = false;
                await _context.SaveChangesAsync();

                var message = $"Product deactivated because it has {string.Join(" and ", messageParts)} associated with it";
                return BadRequest(message);
            }

            // If no orders and no reels, delete completely
            _context.ProductVariants.RemoveRange(product.Variants);
            _context.ProductImages.RemoveRange(product.Images);

            // Remove ReelProducts (should be empty but good practice)
            if (product.ReelProducts?.Any() == true)
            {
                _context.ReelProducts.RemoveRange(product.ReelProducts);
            }

            _context.Products.Remove(product);

            await _context.SaveChangesAsync();

            return Ok("Product deleted successfully");
        }


    }


}
