using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.DTOs.UserDTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Digital_Mall_API.Controllers.User
{
    [ApiController]
    [Route("User/[controller]")]
    public class CatalogController : ControllerBase
    {
        private readonly AppDbContext _context;
        private const int PageSize = 32;

        public CatalogController(AppDbContext context)
        {
            _context = context;
        }
        [HttpGet("search")]
        public async Task<IActionResult> Search(
    [FromQuery] string q, 
    [FromQuery] string? type,
    [FromQuery] string? gender,
    [FromQuery] string? size,
    [FromQuery] decimal? minPrice,
    [FromQuery] decimal? maxPrice,
    [FromQuery] string? sort,
    [FromQuery] int page = 1)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { message = "Search query is required" });

            if (page < 1) page = 1;

            var searchTerm = q.Trim().ToLower();
            var results = new
            {
                Products = new List<object>(),
                Brands = new List<object>(),
                Models = new List<object>()
            };

            if (string.IsNullOrEmpty(type) || type?.ToLower() == "products")
            {
                var productQuery = _context.Products
                    .Include(p => p.Brand)
                    .Include(p => p.SubCategory)
                        .ThenInclude(sc => sc.Category)
                    .Include(p => p.ProductDiscount)
                    .Include(p => p.Images)
                    .Include(p => p.Variants)
                    .Where(p => p.IsActive &&
                               (p.Name.ToLower().Contains(searchTerm) ||
                                p.Description.ToLower().Contains(searchTerm) ||
                                p.Brand.OfficialName.ToLower().Contains(searchTerm) ||
                                p.SubCategory.Name.ToLower().Contains(searchTerm) ||
                                p.SubCategory.Category.Name.ToLower().Contains(searchTerm)))
                    .AsQueryable();

                // Apply filters for products
                if (!string.IsNullOrEmpty(gender))
                {
                    productQuery = productQuery.Where(p => p.Gender.ToLower() == gender.ToLower());
                }

                if (!string.IsNullOrEmpty(size))
                {
                    productQuery = productQuery.Where(p => p.Variants.Any(v => v.Size.ToLower() == size.ToLower()));
                }

                if (minPrice.HasValue)
                    productQuery = productQuery.Where(p => p.Price >= minPrice.Value);
                if (maxPrice.HasValue)
                    productQuery = productQuery.Where(p => p.Price <= maxPrice.Value);

                // Sorting for products
                productQuery = sort?.ToLower() switch
                {
                    "price_asc" => productQuery.OrderBy(p => p.Price),
                    "price_desc" => productQuery.OrderByDescending(p => p.Price),
                    "name_asc" => productQuery.OrderBy(p => p.Name),
                    "name_desc" => productQuery.OrderByDescending(p => p.Name),
                    _ => productQuery.OrderByDescending(p => p.CreatedAt)
                };

                var productTotalItems = await productQuery.CountAsync();
                var products = await productQuery
                    .Skip((page - 1) * PageSize)
                    .Take(PageSize)
                    .Select(p => new DiscountedProductDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        BrandName = p.Brand != null ? p.Brand.OfficialName : null,
                        ImageUrl = p.Images.FirstOrDefault()!.ImageUrl,
                        Description = p.Description,
                        OriginalPrice = p.Price,
                        DiscountValue = p.ProductDiscount != null ? p.ProductDiscount.DiscountValue : 0,
                        DiscountedPrice = p.ProductDiscount != null
                        ? p.Price - (p.Price * p.ProductDiscount.DiscountValue / 100)
                        : p.Price,
                        DiscountStatus = p.ProductDiscount != null ? "Active" : "None",
                        CreatedAt = p.CreatedAt,
                        StockQuantity = p.Variants.Sum(v => v.StockQuantity)
                    })
                    .ToListAsync();

                results.Products.Add(new
                {
                    page,
                    pageSize = PageSize,
                    totalItems = productTotalItems,
                    totalPages = (int)Math.Ceiling(productTotalItems / (double)PageSize),
                    items = products
                });
            }

            // Search Brands
            if (string.IsNullOrEmpty(type) || type?.ToLower() == "brands")
            {
                var brands = await _context.Brands
                    .Where(b => b.Status == "Active" &&
                               (b.OfficialName.ToLower().Contains(searchTerm) ||
                                b.Description.ToLower().Contains(searchTerm)))
                    .OrderBy(b => b.OfficialName)
                    .Select(b => new
                    {
                        b.Id,
                        b.OfficialName,
                        b.LogoUrl,
                        b.Description,
                        ProductsCount = b.Products.Count(p => p.IsActive),
                        b.CreatedAt
                    })
                    .ToListAsync();

                results.Brands.AddRange(brands);
            }

            // Search Models (Fashion Models)
            if (string.IsNullOrEmpty(type) || type?.ToLower() == "models")
            {
                var models = await _context.FashionModels
                    .Where(m => m.Status == "Active" &&
                               (m.Name.ToLower().Contains(searchTerm) ||
                                m.Bio.ToLower().Contains(searchTerm)))
                    .OrderBy(m => m.Name)
                    .Select(m => new
                    {
                        m.Id,
                        m.Name,
                        m.Bio,
                        m.ImageUrl,
                        m.Facebook,
                        m.Instgram,
                        m.OtherSocialAccount,
                        ReelsCount = m.Reels.Count,
                        m.CreatedAt
                    })
                    .ToListAsync();

                results.Models.AddRange(models);
            }

            return Ok(new
            {
                Query = searchTerm,
                Type = type ?? "all",
                Results = results
            });
        }

        // ✅ جلب كل الكاتيجوريز
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.Categories
                .Include(c => c.SubCategories)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.ImageUrl,
                    SubCategories = c.SubCategories.Select(sc => new
                    {
                        sc.Id,
                        sc.Name
                    })
                })
                .ToListAsync();

            return Ok(categories);
        }

        
        [HttpGet("products")]
        public async Task<IActionResult> GetProducts(
            [FromQuery] int? categoryId,
            [FromQuery] string? gender,
            [FromQuery] string? size,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] string? sort, 
            [FromQuery] int page = 1)
        {
            if (page < 1) page = 1;

            var query = _context.Products
                .Include(p => p.Brand)
                .Include(p => p.SubCategory)
                    .ThenInclude(sc => sc.Category)
                .Include(p => p.ProductDiscount)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .AsQueryable();

            // ✅ filter by category
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.SubCategory.Category.Id == categoryId.Value);
            }


            if (!string.IsNullOrEmpty(gender))
            {
                query = query.Where(p => p.Gender.ToLower() == gender.ToLower());
            }

            // ✅ filter by size
            if (!string.IsNullOrEmpty(size))
            {
                query = query.Where(p => p.Variants.Any(v => v.Size.ToLower() == size.ToLower()));
            }

            // ✅ filter by price range
            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);
            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            // ✅ sorting
            query = sort?.ToLower() switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            // ✅ count
            var totalItems = await query.CountAsync();

            // ✅ pagination
            var products = await query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .Select(p => new DiscountedProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    BrandName = p.Brand != null ? p.Brand.OfficialName : null,
                    ImageUrl = p.Images.FirstOrDefault()!.ImageUrl,
                    OriginalPrice = p.Price,
                    DiscountValue = p.ProductDiscount != null ? p.ProductDiscount.DiscountValue : 0,
                    DiscountedPrice = p.ProductDiscount != null ? p.Price - p.ProductDiscount.DiscountValue : p.Price,
                    DiscountStatus = p.ProductDiscount != null ? "Active" : "None",
                    CreatedAt = p.CreatedAt,
                    StockQuantity = p.Variants.Sum(v => v.StockQuantity)
                })
                .ToListAsync();

            return Ok(new
            {
                page,
                pageSize = PageSize,
                totalItems,
                totalPages = (int)Math.Ceiling(totalItems / (double)PageSize),
                products
            });
        }

        [HttpGet("subcategories")]
        public async Task<IActionResult> GetSubCategoriesByCategory(
    [FromQuery] int categoryId)
        {
            var category = await _context.Categories
                .Include(c => c.SubCategories)
                .FirstOrDefaultAsync(c => c.Id == categoryId);

            if (category == null)
                return NotFound(new { message = "Category not found" });

            var subCategories = category.SubCategories
                .Select(sc => new
                {
                    sc.Id,
                    sc.Name,
                    sc.ImageUrl
                })
                .ToList();

            return Ok(new
            {
                Category = new
                {
                    category.Id,
                    category.Name,
                    category.ImageUrl
                },
                SubCategories = subCategories
            });
        }

        [HttpGet("category-details")]
        public async Task<IActionResult> GetCategoryWithProducts(
    [FromQuery] int categoryId,
    [FromQuery] string? gender,
    [FromQuery] string? size,
    [FromQuery] decimal? minPrice,
    [FromQuery] decimal? maxPrice,
    [FromQuery] string? sort, // "price_asc" or "price_desc"
    [FromQuery] int page = 1)
        {
            if (page < 1) page = 1;

            // ✅ أول حاجة نجيب الكاتيجوري بالسب كاتيجوريز
            var category = await _context.Categories
                .Include(c => c.SubCategories)
                .FirstOrDefaultAsync(c => c.Id == categoryId);

            if (category == null)
                return NotFound(new { message = "Category not found" });

            var subCategoryIds = category.SubCategories.Select(sc => sc.Id).ToList();

            // ✅ بعدين نجيب المنتجات اللي تنتمي لأي من الساب كاتيجوريز دي
            var query = _context.Products
                .Include(p => p.Brand)
                .Include(p => p.SubCategory)
                .Include(p => p.ProductDiscount)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .Where(p => subCategoryIds.Contains(p.SubCategoryId))
                .AsQueryable();

            // ✅ apply filters
            if (!string.IsNullOrEmpty(gender))
            {
                query = query.Where(p => p.Gender.ToLower() == gender.ToLower());
            }

            if (!string.IsNullOrEmpty(size))
            {
                query = query.Where(p => p.Variants.Any(v => v.Size.ToLower() == size.ToLower()));
            }

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);
            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            query = sort?.ToLower() switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            var totalItems = await query.CountAsync();

            var products = await query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .Select(p => new DiscountedProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    BrandName = p.Brand != null ? p.Brand.OfficialName : null,
                    ImageUrl = p.Images.FirstOrDefault()!.ImageUrl,
                    OriginalPrice = p.Price,
                    DiscountValue = p.ProductDiscount != null ? p.ProductDiscount.DiscountValue : 0,
                    DiscountedPrice = p.ProductDiscount != null ? p.Price - p.ProductDiscount.DiscountValue : p.Price,
                    DiscountStatus = p.ProductDiscount != null ? "Active" : "None",
                    CreatedAt = p.CreatedAt,
                    StockQuantity = p.Variants.Sum(v => v.StockQuantity)
                })
                .ToListAsync();

            return Ok(new
            {
                Category = new
                {
                    category.Id,
                    category.Name,
                    category.ImageUrl,
                    SubCategories = category.SubCategories.Select(sc => new
                    {
                        sc.Id,
                        sc.Name
                    })
                },
                Products = new
                {
                    page,
                    pageSize = PageSize,
                    totalItems,
                    totalPages = (int)Math.Ceiling(totalItems / (double)PageSize),
                    items = products
                }
            });
        }
        [HttpGet("subcategory-products")]
        public async Task<IActionResult> GetProductsBySubCategory(
    [FromQuery] int subCategoryId,
    [FromQuery] string? gender,
    [FromQuery] string? size,
    [FromQuery] decimal? minPrice,
    [FromQuery] decimal? maxPrice,
    [FromQuery] string? sort, // "price_asc" or "price_desc"
    [FromQuery] int page = 1)
        {
            if (page < 1) page = 1;

            var subCategory = await _context.SubCategories
                .Include(sc => sc.Category)
                .FirstOrDefaultAsync(sc => sc.Id == subCategoryId);

            if (subCategory == null)
                return NotFound(new { message = "SubCategory not found" });

            var query = _context.Products
                .Include(p => p.Brand)
                .Include(p => p.ProductDiscount)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .Where(p => p.SubCategoryId == subCategoryId)
                .AsQueryable();

            // ✅ filters
            if (!string.IsNullOrEmpty(gender))
            {
                query = query.Where(p => p.Gender.ToLower() == gender.ToLower());
            }

            if (!string.IsNullOrEmpty(size))
            {
                query = query.Where(p => p.Variants.Any(v => v.Size.ToLower() == size.ToLower()));
            }

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);
            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            // ✅ sorting
            query = sort?.ToLower() switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            // ✅ pagination
            var totalItems = await query.CountAsync();

            var products = await query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .Select(p => new DiscountedProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    BrandName = p.Brand != null ? p.Brand.OfficialName : null,
                    ImageUrl = p.Images.FirstOrDefault()!.ImageUrl,
                    OriginalPrice = p.Price,
                    DiscountValue = p.ProductDiscount != null ? p.ProductDiscount.DiscountValue : 0,
                    DiscountedPrice = p.ProductDiscount != null ? p.Price - p.ProductDiscount.DiscountValue : p.Price,
                    DiscountStatus = p.ProductDiscount != null ? "Active" : "None",
                    CreatedAt = p.CreatedAt,
                    StockQuantity = p.Variants.Sum(v => v.StockQuantity)
                })
                .ToListAsync();

            return Ok(new
            {
                SubCategory = new
                {
                    subCategory.Id,
                    subCategory.Name,
                    Category = new
                    {
                        subCategory.Category.Id,
                        subCategory.Category.Name,
                        subCategory.Category.ImageUrl
                    }
                },
                Products = new
                {
                    page,
                    pageSize = PageSize,
                    totalItems,
                    totalPages = (int)Math.Ceiling(totalItems / (double)PageSize),
                    items = products
                }
            });
        }
    }
}
