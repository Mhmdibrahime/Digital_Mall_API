using Digital_Mall_API.Controllers.Reels;
using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.DTOs.UserDTOs;
using Digital_Mall_API.Models.DTOs.UserDTOs.ReelsDTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

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
    [FromQuery] string type = "all",
    [FromQuery] string? gender = null,
    [FromQuery] string? size = null,
    [FromQuery] decimal? minPrice =null,
    [FromQuery] decimal? maxPrice = null,
    [FromQuery] string sort = null,
    [FromQuery] int page = 1,
     [FromQuery] int PageSize = 20
    )
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { message = "Search query is required" });

            if (page < 1) page = 1;

            var searchTerm = q.Trim().ToLower();
            var searchType = (type ?? "products").ToLower();

            var results = new
            {
                Products = new List<object>(),
                Brands = new List<object>(),
                Models = new List<object>()
            };

            // SEARCH PRODUCTS (if type is "all" or "products")
            if (searchType == "all" || searchType == "products")
            {
                var productQuery = _context.Products
     .Include(p => p.SubSubCategory)
         .ThenInclude(ssc => ssc.SubCategory)
             .ThenInclude(sc => sc.Category)
     .Where(p => p.IsActive && p.Brand.Status == "Active" &&
                (p.Name.ToLower().Contains(searchTerm) ||
                 p.Description.ToLower().Contains(searchTerm) ||
                 p.Brand.OfficialName.ToLower().Contains(searchTerm) ||
                 p.SubSubCategory.EnglishName.ToLower().Contains(searchTerm) ||
                 p.SubSubCategory.ArabicName.ToLower().Contains(searchTerm) ||
                 p.SubSubCategory.SubCategory.EnglishName.ToLower().Contains(searchTerm) ||
                 p.SubSubCategory.SubCategory.ArabicName.ToLower().Contains(searchTerm) ||
                 p.SubSubCategory.SubCategory.Category.EnglishName.ToLower().Contains(searchTerm) ||
                 p.SubSubCategory.SubCategory.Category.ArabicName.ToLower().Contains(searchTerm)))
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
                    .Select(p => new
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

            // SEARCH BRANDS (if type is "all" or "brands")
            if (searchType == "all" || searchType == "brands")
            {
                var brandQuery = _context.Brands
                    .Where(b => b.Status == "Active" &&
                               (b.OfficialName.ToLower().Contains(searchTerm) ||
                                b.Description.ToLower().Contains(searchTerm)))
                    .OrderBy(b => b.OfficialName);

                var brandTotalItems = await brandQuery.CountAsync();
                var brands = await brandQuery
                    .Skip((page - 1) * PageSize)
                    .Take(PageSize)
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

                results.Brands.Add(new
                {
                    page,
                    pageSize = PageSize,
                    totalItems = brandTotalItems,
                    totalPages = (int)Math.Ceiling(brandTotalItems / (double)PageSize),
                    items = brands
                });
            }

            // SEARCH MODELS (if type is "all" or "models")
            if (searchType == "all" || searchType == "models")
            {
                var modelQuery = _context.FashionModels
                    .Where(m => m.Status == "Active" &&
                               (m.Name.ToLower().Contains(searchTerm) ||
                                m.Bio.ToLower().Contains(searchTerm)))
                    .OrderBy(m => m.Name);

                var modelTotalItems = await modelQuery.CountAsync();
                var models = await modelQuery
                    .Skip((page - 1) * PageSize)
                    .Take(PageSize)
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

                results.Models.Add(new
                {
                    page,
                    pageSize = PageSize,
                    totalItems = modelTotalItems,
                    totalPages = (int)Math.Ceiling(modelTotalItems / (double)PageSize),
                    items = models
                });
            }

            return Ok(new
            {
                Query = searchTerm,
                Type = searchType,
                Page = page,
                PageSize = PageSize,
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
                    c.EnglishName,
                    c.ArabicName,
                    c.ImageUrl,
                    SubCategories = c.SubCategories.Select(sc => new
                    {
                        sc.Id,
                        sc.EnglishName,
                        sc.ArabicName
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
.Include(p => p.SubSubCategory)
            .ThenInclude(ssc => ssc.SubCategory)
                .ThenInclude(sc => sc.Category)
                .Include(p => p.ProductDiscount)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .Where(p => p.IsActive && p.Brand.Status == "Active")
                .AsQueryable();

            // ✅ filter by category
            if (categoryId.HasValue)
                query = query.Where(p => p.SubSubCategory.SubCategory.CategoryId == categoryId.Value);


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
                    sc.EnglishName,
                    sc.ArabicName,
                    sc.ImageUrl
                })
                .ToList();

            return Ok(new
            {
                Category = new
                {
                    category.Id,
                    category.EnglishName,
                    category.ArabicName,
                    category.ImageUrl
                },
                SubCategories = subCategories
            });
        }

        [HttpGet("subsubcategories")]
        public async Task<IActionResult> GetSubSubCategoriesBySubCategory(
    [FromQuery] int subCategoryId)
        {
            var subCategory = await _context.SubCategories
                .Include(sc => sc.SubSubCategories)
                .Include(sc => sc.Category)
                .FirstOrDefaultAsync(sc => sc.Id == subCategoryId);

            if (subCategory == null)
                return NotFound(new { message = "SubCategory not found" });

            var subSubCategories = subCategory.SubSubCategories
                .Select(ssc => new
                {
                    ssc.Id,
                    ssc.EnglishName,
                    ssc.ArabicName,
                    ssc.ImageUrl
                })
                .ToList();

            return Ok(new
            {
                SubCategory = new
                {
                    subCategory.Id,
                    subCategory.EnglishName,
                    subCategory.ArabicName,
                    subCategory.ImageUrl,
                    Category = new
                    {
                        subCategory.Category.Id,
                        subCategory.Category.EnglishName,
                        subCategory.Category.ArabicName,
                        subCategory.Category.ImageUrl
                    }
                },
                SubSubCategories = subSubCategories
            });
        }

        [HttpGet("category-details")]
        public async Task<IActionResult> GetCategoryWithProducts(
   [FromQuery] int categoryId,
    [FromQuery] int? subCategoryId,
    [FromQuery] int? subSubCategoryId,
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
           .ThenInclude(sc => sc.SubSubCategories)
       .FirstOrDefaultAsync(c => c.Id == categoryId);

            if (category == null)
                return NotFound(new { message = "Category not found" });

            var subSubCategoryIds = new List<int>();
            if (subSubCategoryId.HasValue)
                subSubCategoryIds.Add(subSubCategoryId.Value);
            else if (subCategoryId.HasValue)
            {
                var subCat = await _context.SubCategories
                    .Include(sc => sc.SubSubCategories)
                    .FirstOrDefaultAsync(sc => sc.Id == subCategoryId.Value);
                if (subCat != null)
                    subSubCategoryIds = subCat.SubSubCategories.Select(ssc => ssc.Id).ToList();
            }
            else
            {
                subSubCategoryIds = category.SubCategories
                    .SelectMany(sc => sc.SubSubCategories)
                    .Select(ssc => ssc.Id)
                    .ToList();
            }

            // ✅ بعدين نجيب المنتجات اللي تنتمي لأي من الساب كاتيجوريز دي
            var query = _context.Products
                .Include(p => p.Brand)
                .Include(p => p.SubSubCategory)
                .Include(p => p.ProductDiscount)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .Where(p => subSubCategoryIds.Contains((int)p.SubSubCategoryId) && p.IsActive && p.Brand.Status == "Active")

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
                    category.EnglishName,
                    category.ArabicName,
                    category.ImageUrl,
                    SubCategories = category.SubCategories.Select(sc => new
                    {
                        sc.Id,
                        sc.EnglishName,
                        sc.ArabicName
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
                .Include(p => p.SubSubCategory)
                .Where(p => p.SubSubCategory.SubCategoryId == subCategoryId && p.IsActive && p.Brand.Status == "Active" )
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
                    subCategory.EnglishName,
                    subCategory.ArabicName,
                    Category = new
                    {
                        subCategory.Category.Id,
                        subCategory.Category.EnglishName,
                        subCategory.Category.ArabicName,
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
        [HttpGet("subsubcategory-products")]
        public async Task<IActionResult> GetProductsBySubSubCategory(
    [FromQuery] int subSubCategoryId,
    [FromQuery] string? gender,
    [FromQuery] string? size,
    [FromQuery] decimal? minPrice,
    [FromQuery] decimal? maxPrice,
    [FromQuery] string? sort,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;

            // جلب الـ SubSubCategory مع الـ SubCategory والـ Category المرتبطة بها
            var subSubCategory = await _context.SubSubCategories
                .Include(ssc => ssc.SubCategory)
                    .ThenInclude(sc => sc.Category)
                .FirstOrDefaultAsync(ssc => ssc.Id == subSubCategoryId);

            if (subSubCategory == null)
                return NotFound(new { message = "SubSubCategory not found" });

            // بناء الاستعلام لجلب المنتجات النشطة التابعة لهذا الـ SubSubCategory
            var query = _context.Products
                .Include(p => p.Brand)
                .Include(p => p.ProductDiscount)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .Where(p => p.SubSubCategoryId == subSubCategoryId
                            && p.IsActive
                            && p.Brand.Status == "Active")
                .AsQueryable();

            // تطبيق الفلاتر
            if (!string.IsNullOrEmpty(gender))
                query = query.Where(p => p.Gender.ToLower() == gender.ToLower());

            if (!string.IsNullOrEmpty(size))
                query = query.Where(p => p.Variants.Any(v => v.Size.ToLower() == size.ToLower()));

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);
            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            // الترتيب
            query = sort?.ToLower() switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "name_asc" => query.OrderBy(p => p.Name),
                "name_desc" => query.OrderByDescending(p => p.Name),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            // حساب العدد الإجمالي قبل التقسيم
            var totalItems = await query.CountAsync();

            // جلب المنتجات مع التقسيم
            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new DiscountedProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    BrandName = p.Brand != null ? p.Brand.OfficialName : null,
                    ImageUrl = p.Images.FirstOrDefault() != null ? p.Images.First().ImageUrl : null,
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

           
            return Ok(new
            {
                SubSubCategory = new
                {
                    subSubCategory.Id,
                    subSubCategory.EnglishName,
                    subSubCategory.ArabicName,
                    subSubCategory.ImageUrl,
                    SubCategory = new
                    {
                        subSubCategory.SubCategory.Id,
                        subSubCategory.SubCategory.EnglishName,
                        subSubCategory.SubCategory.ArabicName,
                        Category = new
                        {
                            subSubCategory.SubCategory.Category.Id,
                            subSubCategory.SubCategory.Category.EnglishName,
                            subSubCategory.SubCategory.Category.ArabicName,
                            subSubCategory.SubCategory.Category.ImageUrl
                        }
                    }
                },
                Products = new
                {
                    page,
                    pageSize,
                    totalItems,
                    totalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                    items = products
                }
            });
        }
        [HttpGet("random-reels")]
        public async Task<ActionResult<List<RandomReelFeedDto>>> GetRandomReels()
        {
            try
            {
                // Get 10 random active reels
                var randomReels = await _context.Reels
                    .Where(r => r.UploadStatus == "ready" &&
                               (r.PostedByModel.Status == "Active" || r.PostedByBrand.Status == "Active"))
                    .Include(r => r.PostedByModel)
                    .Include(r => r.PostedByBrand)
                    .Include(r => r.LinkedProducts)
                        .ThenInclude(rp => rp.Product)
                        .ThenInclude(p => p.Images)
                    .OrderBy(r => Guid.NewGuid()) // Random order
                    .Take(10)
                    .ToListAsync();

                var reelIds = randomReels.Select(r => r.Id).ToList();

                

                var reelDtos = randomReels.Select(reel => new ReelFeedDto
                {
                    Id = reel.Id,
                    Caption = reel.Caption,
                    VideoUrl = reel.VideoUrl,
                    ThumbnailUrl = reel.ThumbnailUrl,
                    PostedDate = reel.PostedDate,
                    DurationInSeconds = reel.DurationInSeconds,
                    LikesCount = reel.LikesCount,
                    SharesCount = reel.SharesCount,
                    PostedByUserType = reel.PostedByUserType,
                    PostedByUserId = reel.PostedByUserId,
                    PostedByName = reel.PostedByUserType == "FashionModel"
                        ? reel.PostedByModel.Name
                        : reel.PostedByBrand.OfficialName,
                    PostedByImage = reel.PostedByUserType == "FashionModel"
                        ? reel.PostedByModel.ImageUrl
                        : reel.PostedByBrand.LogoUrl,
                    LinkedProducts = reel.LinkedProducts.Where(p=>p.Product.IsActive == true).Select(rp => new ReelProductDto
                    {
                        ProductId = rp.ProductId,
                        ProductName = rp.Product.Name,
                        ProductPrice = rp.Product.Price,
                        ProductImageUrl = rp.Product.Images.Select(i => i.ImageUrl).FirstOrDefault()
                    }).ToList()
                }).ToList();

                return Ok(reelDtos);
            }
            catch (Exception ex)
            {
                // Log the exception if you have a logger
                // _logger.LogError(ex, "Error getting random reels");
                return StatusCode(500, "Error retrieving random reels");
            }
        }

        [HttpGet("reel/{id}")]
        public async Task<ActionResult<RandomReelFeedDto>> GetReelById(int id)
        {
            try
            {
                var reel = await _context.Reels
                    .Where(r => r.Id == id && r.UploadStatus == "ready" &&
                               (r.PostedByModel.Status == "Active" || r.PostedByBrand.Status == "Active"))
                    .Include(r => r.PostedByModel)
                    .Include(r => r.PostedByBrand)
                    .Include(r => r.LinkedProducts)
                        .ThenInclude(rp => rp.Product)
                        .ThenInclude(p => p.Images)
                    .FirstOrDefaultAsync();

                if (reel == null)
                {
                    return NotFound(new { message = "Reel not found" });
                }


              

                var reelDto = new ReelFeedDto
                {
                    Id = reel.Id,
                    Caption = reel.Caption,
                    VideoUrl = reel.VideoUrl,
                    ThumbnailUrl = reel.ThumbnailUrl,
                    PostedDate = reel.PostedDate,
                    DurationInSeconds = reel.DurationInSeconds,
                    LikesCount = reel.LikesCount,
                    SharesCount = reel.SharesCount,
                    PostedByUserType = reel.PostedByUserType,
                    PostedByUserId = reel.PostedByUserId,
                    PostedByName = reel.PostedByUserType == "FashionModel"
                        ? reel.PostedByModel.Name
                        : reel.PostedByBrand.OfficialName,
                    PostedByImage = reel.PostedByUserType == "FashionModel"
                        ? reel.PostedByModel.ImageUrl
                        : reel.PostedByBrand.LogoUrl,
                    LinkedProducts = reel.LinkedProducts.Where(p=>p.Product.IsActive == true).Select(rp => new ReelProductDto
                    {
                        ProductId = rp.ProductId,
                        ProductName = rp.Product.Name,
                        ProductPrice = rp.Product.Price,
                        ProductImageUrl = rp.Product.Images.Select(i => i.ImageUrl).FirstOrDefault()
                    }).ToList()
                };

                return Ok(reelDto);
            }
            catch (Exception ex)
            {
                // Log the exception if you have a logger
                // _logger.LogError(ex, $"Error getting reel with ID {id}");
                return StatusCode(500, "Error retrieving reel details");
            }
        }

        
    }
}
