using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.Entities.Product_Catalog;
using System.ComponentModel.DataAnnotations;
using Digital_Mall_API.Models.DTOs.SuperAdminDTOs.CategoriesManagementDTOs;

namespace Digital_Mall_API.Controllers.SuperAdmin
{
    [Route("Super/[controller]")]
    [ApiController]
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
                        c.Name.Contains(search) ||
                        c.Description.Contains(search) ||
                        c.SubCategories.Any(sc => sc.Name.Contains(search))
                    );
                }

                var categories = await query
                    .OrderBy(c => c.Name)
                    .Select(c => new CategoryResponseDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Description = c.Description,
                        ImageUrl = c.ImageUrl,
                        TotalProducts = c.SubCategories.Sum(sc =>
                            _context.Products.Count(p => p.SubCategoryId == sc.Id && p.IsActive)),
                        SubCategories = c.SubCategories.Select(sc => new SubCategoryResponseDto
                        {
                            Id = sc.Id,
                            Name = sc.Name,
                            ImageUrl = sc.ImageUrl,
                            CategoryId = sc.CategoryId,
                            CategoryName = c.Name,
                            ProductCount = _context.Products.Count(p => p.SubCategoryId == sc.Id && p.IsActive)
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
                    query = query.Where(sc => sc.Name.Contains(search));
                }

                var subCategories = await query
                    .OrderBy(sc => sc.Name)
                    .Select(sc => new AvailableSubCategoryDto
                    {
                        Id = sc.Id,
                        Name = sc.Name,
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
                    .OrderBy(c => c.Name)
                    .Select(c => new
                    {
                        Id = c.Id,
                        Name = c.Name,
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
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == request.Name.ToLower());

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
                    Name = request.Name,
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
                        Name = c.Name,
                        Description = c.Description,
                        ImageUrl = c.ImageUrl,
                        
                        SubCategories = c.SubCategories.Select(sc => new SubCategoryResponseDto
                        {
                            Id = sc.Id,
                            Name = sc.Name,
                            ImageUrl = sc.ImageUrl,
                            CategoryId = sc.CategoryId,
                            CategoryName = c.Name,
                            ProductCount = _context.Products.Count(p => p.SubCategoryId == sc.Id && p.IsActive)
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
                var category = await _context.Categories.FindAsync(request.CategoryId);
                if (category == null)
                {
                    return NotFound(new { message = "Selected category not found." });
                }

                var existingSubCategory = await _context.SubCategories
                    .FirstOrDefaultAsync(sc =>
                        sc.CategoryId == request.CategoryId &&
                        sc.Name.ToLower() == request.Name.ToLower());

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
                    Name = request.Name,
                    CategoryId = request.CategoryId,
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
                        Name = sc.Name,
                        ImageUrl = sc.ImageUrl,
                        CategoryId = sc.CategoryId,
                        CategoryName = sc.Category.Name,
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
                        Name = c.Name,
                        Description = c.Description,
                        ImageUrl = c.ImageUrl,
                        TotalProducts = c.SubCategories.Sum(sc =>
                            _context.Products.Count(p => p.SubCategoryId == sc.Id && p.IsActive)),
                        SubCategories = c.SubCategories.Select(sc => new SubCategoryResponseDto
                        {
                            Id = sc.Id,
                            Name = sc.Name,
                            ImageUrl = sc.ImageUrl,
                            CategoryId = sc.CategoryId,
                            CategoryName = c.Name,
                            ProductCount = _context.Products.Count(p => p.SubCategoryId == sc.Id && p.IsActive)
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
    }
}