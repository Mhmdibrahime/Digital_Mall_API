using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.DTOs.UserDTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Polly;

namespace Digital_Mall_API.Controllers.User
{
    [Route("User/[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly AppDbContext context;

        public HomeController(AppDbContext context)
        {
            this.context = context;
        }
        [HttpGet("Categoies")]
        public async Task<IActionResult> GetCategoriesSimple()
        {
            var categories = await context.Categories
                .Select(c => new CategoryHomeDto
                {
                    Id = c.Id,
                    Image = c.ImageUrl,
                    Name=c.Name

                })
                .ToListAsync();

            return Ok(categories);
        }

        [HttpGet("top-brands")]
        public async Task<IActionResult> GetTopBrandsBySales()
        {
            var topBrands = await context.OrderItems
                .AsNoTracking()
               
                .Where(oi => oi.BrandId != null &&
                             (oi.Order.Status == "Completed" || oi.Order.Status == "Deliverd") &&
                             oi.Order.PaymentStatus == "Paid")
                .GroupBy(oi => new
                {
                    oi.BrandId,
                    oi.Brand.OfficialName,
                    oi.Brand.LogoUrl
                })
                .Select(g => new
                {
                    BrandId = g.Key.BrandId,
                    BrandName = g.Key.OfficialName,
                    Logo = g.Key.LogoUrl,
                    TotalSales = g.Sum(x => x.Quantity * x.PriceAtTimeOfPurchase),
                    TotalOrders = g.Select(x => x.OrderId).Distinct().Count()
                })
                .OrderByDescending(x => x.TotalSales)
                .Take(10)
                .ToListAsync();

            return Ok(topBrands);
        }
        [HttpGet("top-discounted")]
        public async Task<IActionResult> GetTopDiscountedProducts()
        {
            var products = await context.Products
                .Include(p => p.ProductDiscount)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .Include(p => p.Images)
                .Where(p => p.ProductDiscount != null &&
                            p.ProductDiscount.Status == "Active" &&
                            p.IsActive)
                .OrderByDescending(p => p.ProductDiscount.DiscountValue)
                .Take(12)
                .Select(p => new DiscountedProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    BrandName = p.Brand != null ? p.Brand.OfficialName : null,
                    ImageUrl = p.Images.FirstOrDefault() != null ? p.Images.FirstOrDefault().ImageUrl : null,
                    OriginalPrice = p.Price,
                    DiscountValue = p.ProductDiscount.DiscountValue,
                    DiscountedPrice = p.Price - (p.Price * p.ProductDiscount.DiscountValue / 100),
                    DiscountStatus = p.ProductDiscount.Status,
                    CreatedAt = p.ProductDiscount.CreatedAt,
                    StockQuantity = p.Variants.Sum(v => v.StockQuantity)
                })
                .ToListAsync();

            return Ok(products);
        }
        [HttpGet("top-selling")]
        public IActionResult GetTopSellingProducts()
        {
            var topSellingProducts = context.OrderItems
                .Include(oi => oi.ProductVariant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.Brand)
                .Include(oi => oi.ProductVariant.Product.Images)
                .Include(oi => oi.ProductVariant.Product.ProductDiscount)
                .Include(oi => oi.ProductVariant.Product.Variants)
                .Where(oi => oi.ProductVariant != null &&
                             oi.ProductVariant.Product != null &&
                             oi.ProductVariant.Product.IsActive)
                .AsEnumerable() 
                .GroupBy(oi => oi.ProductVariant.Product)
                .Select(g => new
                {
                    Product = g.Key,
                    TotalSold = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(12)
                .Select(x => new DiscountedProductDto
                {
                    Id = x.Product.Id,
                    Name = x.Product.Name,
                    BrandName = x.Product.Brand?.OfficialName,
                    ImageUrl = x.Product.Images.FirstOrDefault()?.ImageUrl, 
                    OriginalPrice = x.Product.Price,
                    DiscountValue = x.Product.ProductDiscount?.DiscountValue ?? 0,
                    DiscountedPrice = x.Product.ProductDiscount != null
                        ? x.Product.Price - (x.Product.Price * x.Product.ProductDiscount.DiscountValue / 100)
                        : x.Product.Price,
                    DiscountStatus = x.Product.ProductDiscount?.Status ?? "None",
                    CreatedAt = x.Product.CreatedAt,
                    StockQuantity = x.Product.Variants.Sum(v => v.StockQuantity)
                })
                .ToList();

            return Ok(topSellingProducts);
        }

        [HttpGet("trending")]
        public async Task<IActionResult> GetTrendingProducts()
        {
            var trendingProducts = await context.Products
                .Include(p => p.Brand)
                .Include(p => p.Images)
                .Include(p => p.ProductDiscount)
                .Include(p => p.Variants)
                .Where(p => p.IsActive && p.IsTrend)
                .OrderByDescending(p => p.CreatedAt)
                .Take(12)
                .Select(p => new DiscountedProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    BrandName = p.Brand != null ? p.Brand.OfficialName : null,
                    ImageUrl = p.Images.FirstOrDefault() != null ? p.Images.FirstOrDefault().ImageUrl : null,
                    OriginalPrice = p.Price,
                    DiscountValue = p.ProductDiscount != null ? p.ProductDiscount.DiscountValue : 0,
                    DiscountedPrice = p.ProductDiscount != null
                        ? p.Price - (p.Price * p.ProductDiscount.DiscountValue / 100)
                        : p.Price,
                    DiscountStatus = p.ProductDiscount != null ? p.ProductDiscount.Status : "None",
                    CreatedAt = p.CreatedAt,
                    StockQuantity = p.Variants.Sum(v => v.StockQuantity)
                })
                .ToListAsync();

            return Ok(trendingProducts);
        }
    }
}
