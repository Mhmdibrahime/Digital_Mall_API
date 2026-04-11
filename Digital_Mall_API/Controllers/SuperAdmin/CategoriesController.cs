using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.DTOs.SuperAdminDTOs.CategoriesManagementDTOs;
using Digital_Mall_API.Models.Entities.Product_Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Intrinsics.X86;

namespace Digital_Mall_API.Controllers.SuperAdmin
{
    [Route("Super/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]

    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public CategoriesController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<CategoryResponseDto>>> SearchCategories([FromQuery] string search = "")
        {
            try
            {
                var query = _context.Categories
                    .Include(c => c.SubCategories)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(c =>
                        c.EnglishName.Contains(search) ||
                        c.Description.Contains(search) ||
                        c.SubCategories.Any(sc => sc.EnglishName.Contains(search))
                    );
                }

                var categories = await query
                    .OrderBy(c => c.EnglishName)
                    .Select(c => new CategoryResponseDto
                    {
                        Id = c.Id,
                        Name = c.EnglishName,
                        Description = c.Description,
                        ImageUrl = c.ImageUrl,
                        TotalProducts = c.SubCategories.Sum(sc =>
                            _context.Products.Count(p => p.SubSubCategory.SubCategoryId == sc.Id && p.IsActive)),
                        SubCategories = c.SubCategories.Select(sc => new SubCategoryResponseDto
                        {
                            Id = sc.Id,
                            Name = sc.EnglishName,
                            ArabicName = sc.ArabicName,
                            ImageUrl = sc.ImageUrl,
                            CategoryId = sc.CategoryId,
                            CategoryName = c.EnglishName,
                            ProductCount = _context.Products.Count(p => p.SubSubCategory.SubCategoryId == sc.Id && p.IsActive)
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(categories);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while searching categories", error = ex.Message });
            }
        }

        [HttpGet("available-subcategories")]
        public async Task<ActionResult<IEnumerable<AvailableSubCategoryDto>>> GetAvailableSubCategories([FromQuery] string search = "")
        {
            try
            {
                var query = _context.SubCategories
                    .Include(sc => sc.Category)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(sc => sc.EnglishName.Contains(search) || sc.ArabicName.Contains(search));
                }

                var subCategories = await query
                    .OrderBy(sc => sc.EnglishName)
                    .Select(sc => new AvailableSubCategoryDto
                    {
                        Id = sc.Id,
                        Name = sc.EnglishName,
                        ArabicName = sc.ArabicName,
                        ImageUrl = sc.ImageUrl,
                    })
                    .ToListAsync();

                return Ok(subCategories);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving subcategories", error = ex.Message });
            }
        }

        [HttpGet("all-categories")]
        public async Task<ActionResult<IEnumerable<object>>> GetAllCategoriesForDropdown()
        {
            try
            {
                var categories = await _context.Categories
                    .Where(c => c.SubCategories.Any())
                    .OrderBy(c => c.EnglishName)
                    .ThenBy(c => c.ArabicName)
                    .Select(c => new
                    {
                        Id = c.Id,
                        Name = c.EnglishName,
                        ArabicName = c.ArabicName,
                        Description = c.Description,
                        ImageUrl = c.ImageUrl,
                        HasSubCategories = c.SubCategories.Any()
                    })
                    .ToListAsync();

                return Ok(categories);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving categories", error = ex.Message });
            }
        }

        [HttpPost("createCategory")]
        public async Task<ActionResult<CategoryResponseDto>> CreateCategoryWithPopup([FromForm] CreateCategoryPopupDto request)
        {
            try
            {
                var existingCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.EnglishName.ToLower() == request.Name.ToLower() || c.ArabicName == request.ArabicName);

                if (existingCategory != null)
                {
                    return Conflict(new { message = "A category with this name already exists." });
                }

                if (request.Image == null || request.Image.Length == 0)
                {
                    return BadRequest(new { message = "Category image is required." });
                }

                var imageUrl = await SaveImage(request.Image, "categories");
                if (string.IsNullOrEmpty(imageUrl))
                {
                    return BadRequest(new { message = "Failed to save category image." });
                }

                var category = new Category
                {
                    EnglishName = request.Name,
                    ArabicName = request.ArabicName,
                    Description = request.Description ?? string.Empty,
                    ImageUrl = imageUrl
                };

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                if (request.SubCategoryIds != null && request.SubCategoryIds.Any())
                {
                    var subCategoriesToLink = await _context.SubCategories
                        .Where(sc => request.SubCategoryIds.Contains(sc.Id))
                        .ToListAsync();

                    foreach (var subCategory in subCategoriesToLink)
                    {
                        subCategory.CategoryId = category.Id;
                    }

                    await _context.SaveChangesAsync();
                }

                var createdCategory = await _context.Categories
                    .Include(c => c.SubCategories)
                    .Where(c => c.Id == category.Id)
                    .Select(c => new CategoryResponseDto
                    {
                        Id = c.Id,
                        Name = c.EnglishName,
                        ArabicName = c.ArabicName,
                        Description = c.Description,
                        ImageUrl = c.ImageUrl,

                        SubCategories = c.SubCategories.Select(sc => new SubCategoryResponseDto
                        {
                            Id = sc.Id,
                            Name = sc.EnglishName,
                            ArabicName = sc.ArabicName,
                            ImageUrl = sc.ImageUrl,
                            CategoryId = sc.CategoryId,
                            CategoryName = c.EnglishName,
                            ProductCount = _context.Products.Count(p => p.SubSubCategoryId == sc.Id && p.IsActive)
                        }).ToList(),
                    })
                    .FirstOrDefaultAsync();

                return CreatedAtAction(nameof(GetCategoryById), new { id = category.Id }, createdCategory);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while creating the category",
                    error = ex.Message
                });
            }
        }

        [HttpPost("createSubcategory")]
        public async Task<ActionResult<SubCategoryResponseDto>> CreateSubCategoryWithPopup([FromForm] CreateSubCategoryPopupDto request)
        {
            try
            {
                // Use helper method to get the category ID whether the provided ID is category or subcategory
                var resolvedCategoryId = await ResolveCategoryId(request.CategoryId);

                if (!resolvedCategoryId.HasValue)
                {
                    return NotFound(new { message = "Selected category or subcategory not found." });
                }

                var category = await _context.Categories.FindAsync(resolvedCategoryId.Value);
                if (category == null)
                {
                    return NotFound(new { message = "Category not found." });
                }

                var existingSubCategory = await _context.SubCategories
                    .FirstOrDefaultAsync(sc =>
                        sc.CategoryId == resolvedCategoryId.Value &&
                        sc.EnglishName.ToLower() == request.Name.ToLower() || sc.ArabicName.ToLower() == request.ArabicName.ToLower());

                if (existingSubCategory != null)
                {
                    return Conflict(new { message = "A subcategory with this name already exists in the selected category." });
                }

                string imageUrl = null;
                if (request.Image != null && request.Image.Length > 0)
                {
                    imageUrl = await SaveImage(request.Image, "subcategories");
                }

                var subCategory = new SubCategory
                {
                    EnglishName = request.Name,
                    ArabicName = request.ArabicName,
                    CategoryId = resolvedCategoryId.Value,
                    ImageUrl = imageUrl
                };

                _context.SubCategories.Add(subCategory);
                await _context.SaveChangesAsync();

                var createdSubCategory = await _context.SubCategories
                    .Include(sc => sc.Category)
                    .Where(sc => sc.Id == subCategory.Id)
                    .Select(sc => new SubCategoryResponseDto
                    {
                        Id = sc.Id,
                        Name = sc.EnglishName,
                        ArabicName = sc.ArabicName,
                        ImageUrl = sc.ImageUrl,
                        CategoryId = sc.CategoryId,
                        CategoryName = sc.Category.EnglishName,
                        ProductCount = 0
                    })
                    .FirstOrDefaultAsync();

                return Ok(createdSubCategory);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while creating the subcategory",
                    error = ex.Message
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryResponseDto>> GetCategoryById(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.SubCategories)
                    .Where(c => c.Id == id)
                    .Select(c => new CategoryResponseDto
                    {
                        Id = c.Id,
                        Name = c.EnglishName,
                        ArabicName = c.ArabicName,
                        Description = c.Description,
                        ImageUrl = c.ImageUrl,
                        TotalProducts = c.SubCategories.Sum(sc =>
                            _context.Products.Count(p => p.SubSubCategoryId == sc.Id && p.IsActive)),
                        SubCategories = c.SubCategories.Select(sc => new SubCategoryResponseDto
                        {
                            Id = sc.Id,
                            Name = sc.EnglishName,
                            ArabicName = sc.ArabicName,
                            ImageUrl = sc.ImageUrl,
                            CategoryId = sc.CategoryId,
                            CategoryName = c.EnglishName,
                            ProductCount = _context.Products.Count(p => p.SubSubCategoryId == sc.Id && p.IsActive)
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (category == null)
                {
                    return NotFound(new { message = "Category not found" });
                }

                return Ok(category);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the category", error = ex.Message });
            }
        }

        [HttpPut("edit-category/{id}")]
        public async Task<ActionResult<CategoryResponseDto>> EditCategory(int id, [FromForm] EditCategoryDto request)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.SubCategories)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                {
                    return NotFound(new { message = "Category not found" });
                }

                // Check if name already exists (excluding current category)
                var existingCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.EnglishName.ToLower() == request.Name.ToLower() && c.Id != id && c.ArabicName == request.ArabicName);

                if (existingCategory != null)
                {
                    return Conflict(new { message = "A category with this name already exists." });
                }

                // Update category properties
                category.EnglishName = request.Name;
                category.ArabicName = request.ArabicName;
                category.Description = request.Description ?? string.Empty;

                // Handle image update if new image is provided
                if (request.Image != null && request.Image.Length > 0)
                {
                    var imageUrl = await SaveImage(request.Image, "categories");
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        // Delete old image if exists
                        if (!string.IsNullOrEmpty(category.ImageUrl))
                        {
                            DeleteOldImage(category.ImageUrl);
                        }
                        category.ImageUrl = imageUrl;
                    }
                }

                // Handle subcategory assignments
                if (request.SubCategoryIds != null)
                {
                    await UpdateCategorySubcategories(category, request.SubCategoryIds);
                }

                await _context.SaveChangesAsync();

                var updatedCategory = await _context.Categories
                    .Include(c => c.SubCategories)
                    .Where(c => c.Id == category.Id)
                    .Select(c => new CategoryResponseDto
                    {
                        Id = c.Id,
                        Name = c.EnglishName,
                        ArabicName = c.ArabicName,
                        Description = c.Description,
                        ImageUrl = c.ImageUrl,
                        TotalProducts = c.SubCategories.Sum(sc =>
                            _context.Products.Count(p => p.SubSubCategoryId == sc.Id && p.IsActive)),
                        SubCategories = c.SubCategories.Select(sc => new SubCategoryResponseDto
                        {
                            Id = sc.Id,
                            Name = sc.EnglishName,
                            ArabicName = sc.ArabicName,
                            ImageUrl = sc.ImageUrl,
                            CategoryId = sc.CategoryId,
                            CategoryName = c.EnglishName,
                            ProductCount = _context.Products.Count(p => p.SubSubCategoryId == sc.Id && p.IsActive)
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                return Ok(updatedCategory);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while updating the category",
                    error = ex.Message
                });
            }
        }

        [HttpPut("edit-subcategory/{id}")]
        public async Task<ActionResult<SubCategoryResponseDto>> EditSubCategory(int id, [FromForm] EditSubCategoryDto request)
        {
            try
            {
                var subCategory = await _context.SubCategories
                    .Include(sc => sc.Category)
                    .FirstOrDefaultAsync(sc => sc.Id == id);

                if (subCategory == null)
                {
                    return NotFound(new { message = "Subcategory not found" });
                }

                // Use helper method to resolve the category ID from the request
                var resolvedCategoryId = await ResolveCategoryId(request.CategoryId);

                if (!resolvedCategoryId.HasValue)
                {
                    return NotFound(new { message = "Selected category or subcategory not found." });
                }

                // Check if name already exists in the resolved category (excluding current subcategory)
                var existingSubCategory = await _context.SubCategories
                    .FirstOrDefaultAsync(sc =>
                        sc.CategoryId == resolvedCategoryId.Value &&
                        sc.EnglishName.ToLower() == request.Name.ToLower() &&
                        sc.ArabicName.ToLower() == request.ArabicName.ToLower() &&
                        sc.Id != id);

                if (existingSubCategory != null)
                {
                    return Conflict(new { message = "A subcategory with this name already exists in the selected category." });
                }

                // Update subcategory properties with resolved category ID
                subCategory.EnglishName = request.Name;
                subCategory.ArabicName = request.ArabicName;
                subCategory.CategoryId = resolvedCategoryId.Value;

                // Handle image update if new image is provided
                if (request.Image != null && request.Image.Length > 0)
                {
                    var imageUrl = await SaveImage(request.Image, "subcategories");
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        // Delete old image if exists
                        if (!string.IsNullOrEmpty(subCategory.ImageUrl))
                        {
                            DeleteOldImage(subCategory.ImageUrl);
                        }
                        subCategory.ImageUrl = imageUrl;
                    }
                }
                else if (request.RemoveImage)
                {
                    // Remove existing image if requested
                    if (!string.IsNullOrEmpty(subCategory.ImageUrl))
                    {
                        DeleteOldImage(subCategory.ImageUrl);
                        subCategory.ImageUrl = null;
                    }
                }

                await _context.SaveChangesAsync();

                var updatedSubCategory = await _context.SubCategories
                    .Include(sc => sc.Category)
                    .Where(sc => sc.Id == subCategory.Id)
                    .Select(sc => new SubCategoryResponseDto
                    {
                        Id = sc.Id,
                        Name = sc.EnglishName,
                        ArabicName = sc.ArabicName,
                        ImageUrl = sc.ImageUrl,
                        CategoryId = sc.CategoryId,
                        CategoryName = sc.Category.EnglishName,
                        ProductCount = _context.Products.Count(p => p.SubSubCategoryId == sc.Id && p.IsActive)
                    })
                    .FirstOrDefaultAsync();

                return Ok(updatedSubCategory);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while updating the subcategory",
                    error = ex.Message
                });
            }
        }

        [HttpGet("subcategory/{id}/details")]
        public async Task<ActionResult<SubCategoryDetailDto>> GetSubCategoryDetails(int id)
        {
            try
            {
                var subCategory = await _context.SubCategories
                    .Include(sc => sc.Category)
                    .Where(sc => sc.Id == id)
                    .Select(sc => new SubCategoryDetailDto
                    {
                        Id = sc.Id,
                        Name = sc.EnglishName,
                        ArabicName = sc.ArabicName,
                        ImageUrl = sc.ImageUrl,
                        CategoryId = sc.CategoryId,
                        CategoryName = sc.Category.EnglishName,
                        ProductCount = _context.Products.Count(p => p.SubSubCategoryId == sc.Id && p.IsActive),
                        HasImage = !string.IsNullOrEmpty(sc.ImageUrl)
                    })
                    .FirstOrDefaultAsync();

                if (subCategory == null)
                {
                    return NotFound(new { message = "Subcategory not found" });
                }

                return Ok(subCategory);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while retrieving subcategory details",
                    error = ex.Message
                });
            }
        }

        [HttpPost("createSubSubCategory")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<SubSubCategoryResponseDto>> CreateSubSubCategory([FromForm] CreateSubSubCategoryPopupDto dto)
        {
            try
            {
                // التحقق من وجود الـ SubCategory
                var subCategory = await _context.SubCategories
                    .Include(sc => sc.Category)
                    .FirstOrDefaultAsync(sc => sc.Id == dto.SubCategoryId);
                if (subCategory == null)
                    return NotFound(new { message = "SubCategory not found." });

                // التحقق من عدم وجود اسم مكرر تحت نفس الـ SubCategory
                var existing = await _context.SubSubCategories
                    .FirstOrDefaultAsync(ssc => ssc.SubCategoryId == dto.SubCategoryId &&
                        (ssc.EnglishName.ToLower() == dto.Name.ToLower() ||
                         (dto.ArabicName != null && ssc.ArabicName.ToLower() == dto.ArabicName.ToLower())));
                if (existing != null)
                    return Conflict(new { message = "SubSubCategory with same name already exists under this SubCategory." });

                string? imageUrl = null;
                if (dto.Image != null && dto.Image.Length > 0)
                    imageUrl = await SaveImage(dto.Image, "subsubcategories");

                var subSubCategory = new SubSubCategory
                {
                    EnglishName = dto.Name,
                    ArabicName = dto.ArabicName,
                    ImageUrl = imageUrl,
                    SubCategoryId = dto.SubCategoryId
                };

                _context.SubSubCategories.Add(subSubCategory);
                await _context.SaveChangesAsync();

                var response = new SubSubCategoryResponseDto
                {
                    Id = subSubCategory.Id,
                    Name = subSubCategory.EnglishName,
                    ArabicName = subSubCategory.ArabicName,
                  
                    ImageUrl = subSubCategory.ImageUrl,
                    SubCategoryId = subSubCategory.SubCategoryId,
                    SubCategoryName = subCategory.EnglishName,
                    ProductCount = 0
                };

                return CreatedAtAction(nameof(GetSubSubCategoryById), new { id = subSubCategory.Id }, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating subsubcategory", error = ex.Message });
            }
        }

        [HttpGet("subSubCategory/{id}")]
        public async Task<ActionResult<SubSubCategoryResponseDto>> GetSubSubCategoryById(int id)
        {
            var subSubCategory = await _context.SubSubCategories
                .Include(ssc => ssc.SubCategory)
                .ThenInclude(sc => sc.Category)
                .FirstOrDefaultAsync(ssc => ssc.Id == id);
            if (subSubCategory == null)
                return NotFound();

            var productCount = await _context.Products
                .CountAsync(p => p.SubSubCategoryId == id && p.IsActive); // سنضيف SubSubCategoryId لـ Product لاحقاً

            return new SubSubCategoryResponseDto
            {
                Id = subSubCategory.Id,
                Name = subSubCategory.EnglishName,
                ArabicName = subSubCategory.ArabicName,
                ImageUrl = subSubCategory.ImageUrl,
                SubCategoryId = subSubCategory.SubCategoryId,
                SubCategoryName = subSubCategory.SubCategory?.EnglishName,
                ProductCount = productCount
            };
        }

        [HttpGet("subSubCategories/bySubCategory/{subCategoryId}")]
        public async Task<ActionResult<IEnumerable<SubSubCategoryResponseDto>>> GetSubSubCategoriesBySubCategory(int subCategoryId)
        {
            var subCategoryExists = await _context.SubCategories.AnyAsync(sc => sc.Id == subCategoryId);
            if (!subCategoryExists)
                return NotFound(new { message = "SubCategory not found." });

            var subSubCategories = await _context.SubSubCategories
                .Where(ssc => ssc.SubCategoryId == subCategoryId)
                .Include(ssc => ssc.SubCategory)
                .Select(ssc => new SubSubCategoryResponseDto
                {
                    Id = ssc.Id,
                    Name = ssc.EnglishName,
                    ArabicName = ssc.ArabicName,
                   
                    ImageUrl = ssc.ImageUrl,
                    SubCategoryId = ssc.SubCategoryId,
                    SubCategoryName = ssc.SubCategory!.EnglishName,
                    ProductCount = _context.Products.Count(p => p.SubSubCategoryId == ssc.Id && p.IsActive)
                })
                .ToListAsync();

            return Ok(subSubCategories);
        }

        [HttpPut("updateSubSubCategory/{id}")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<SubSubCategoryResponseDto>> UpdateSubSubCategory(int id, [FromForm] UpdateSubSubCategoryDto dto)
        {
            try
            {
                var subSubCategory = await _context.SubSubCategories
                    .Include(ssc => ssc.SubCategory)
                    .FirstOrDefaultAsync(ssc => ssc.Id == id);
                if (subSubCategory == null)
                    return NotFound();

                if (!string.IsNullOrEmpty(dto.Name))
                    subSubCategory.EnglishName = dto.Name;
                if (dto.ArabicName != null)
                    subSubCategory.ArabicName = dto.ArabicName;
                if (dto.Description != null)
                    subSubCategory.Description = dto.Description;

                // معالجة الصورة
                if (dto.RemoveImage && !string.IsNullOrEmpty(subSubCategory.ImageUrl))
                {
                    DeleteOldImage(subSubCategory.ImageUrl);
                    subSubCategory.ImageUrl = null;
                }
                if (dto.Image != null && dto.Image.Length > 0)
                {
                    if (!string.IsNullOrEmpty(subSubCategory.ImageUrl))
                        DeleteOldImage(subSubCategory.ImageUrl);
                    subSubCategory.ImageUrl = await SaveImage(dto.Image, "subsubcategories");
                }

                await _context.SaveChangesAsync();

                var productCount = await _context.Products.CountAsync(p => p.SubSubCategoryId == id && p.IsActive);
                return new SubSubCategoryResponseDto
                {
                    Id = subSubCategory.Id,
                    Name = subSubCategory.EnglishName,
                    ArabicName = subSubCategory.ArabicName,
                   
                    ImageUrl = subSubCategory.ImageUrl,
                    SubCategoryId = subSubCategory.SubCategoryId,
                    SubCategoryName = subSubCategory.SubCategory?.EnglishName,
                    ProductCount = productCount
                };
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating subsubcategory", error = ex.Message });
            }
        }

        [HttpDelete("deleteSubSubCategory/{id}")]
        public async Task<ActionResult> DeleteSubSubCategory(int id)
        {
            var subSubCategory = await _context.SubSubCategories
                .Include(ssc => ssc.Products)
                .FirstOrDefaultAsync(ssc => ssc.Id == id);
            if (subSubCategory == null)
                return NotFound();

            if (subSubCategory.Products != null && subSubCategory.Products.Any(p => p.IsActive))
                return BadRequest(new { message = "Cannot delete subsubcategory because it has active products." });

            // حذف الصورة الفعلية
            if (!string.IsNullOrEmpty(subSubCategory.ImageUrl))
                DeleteOldImage(subSubCategory.ImageUrl);

            _context.SubSubCategories.Remove(subSubCategory);
            await _context.SaveChangesAsync();

            return Ok(new { message = "SubSubCategory deleted successfully." });
        }

        #region Helper Methods

        /// <summary>
        /// Helper method to resolve whether the provided ID is a category ID or subcategory ID
        /// and return the appropriate category ID
        /// </summary>
        /// <param name="id">The ID to check (could be category ID or subcategory ID)</param>
        /// <returns>The category ID if found, null otherwise</returns>
        private async Task<int?> ResolveCategoryId(int id)
        {
            // First check if it's a category
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                return category.Id;
            }

            // If not a category, check if it's a subcategory
            var subCategory = await _context.SubCategories
                .Include(sc => sc.Category)
                .FirstOrDefaultAsync(sc => sc.Id == id);

            if (subCategory?.Category != null)
            {
                return subCategory.Category.Id;
            }

            // Not found as either category or subcategory
            return null;
        }

        /// <summary>
        /// Alternative helper method that returns both the resolved ID and the type
        /// </summary>
        private async Task<(int? CategoryId, string EntityType)> ResolveIdType(int id)
        {
            // Check if it's a category
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                return (category.Id, "Category");
            }

            // Check if it's a subcategory
            var subCategory = await _context.SubCategories
                .Include(sc => sc.Category)
                .FirstOrDefaultAsync(sc => sc.Id == id);

            if (subCategory?.Category != null)
            {
                return (subCategory.Category.Id, "SubCategory");
            }

            return (null, "Unknown");
        }

        private async Task UpdateCategorySubcategories(Category category, List<int> newSubCategoryIds)
        {
            // Get current subcategories
            var currentSubCategoryIds = category.SubCategories.Select(sc => sc.Id).ToList();

            // Subcategories to add to this category
            var subCategoriesToAdd = await _context.SubCategories
                .Where(sc => newSubCategoryIds.Contains(sc.Id) && !currentSubCategoryIds.Contains(sc.Id))
                .ToListAsync();

            // Add subcategories to this category
            foreach (var subCategory in subCategoriesToAdd)
            {
                subCategory.CategoryId = category.Id;
            }
        }

        private void DeleteOldImage(string imageUrl)
        {
            try
            {
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    var fullPath = Path.Combine(_environment.WebRootPath, imageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't throw - we don't want image deletion failure to break the main operation
                Console.WriteLine($"Warning: Failed to delete old image: {ex.Message}");
            }
        }

        private async Task<string> SaveImage(IFormFile image, string folder)
        {
            try
            {
                if (image == null || image.Length == 0)
                    return null;

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    throw new InvalidOperationException("Invalid file type. Only JPG, JPEG, PNG, GIF, and WebP files are allowed.");
                }

                if (image.Length > 5 * 1024 * 1024)
                {
                    throw new InvalidOperationException("File size too large. Maximum size is 5MB.");
                }

                var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", folder);
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                return $"/images/{folder}/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save image: {ex.Message}", ex);
            }
        }

        #endregion


    }
}