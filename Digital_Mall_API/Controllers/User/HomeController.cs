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
                .Where(oi => oi.ProductVariant.Product.BrandId != null)
                .GroupBy(oi => new
                {
                    oi.ProductVariant.Product.BrandId,
                    oi.ProductVariant.Product.Brand.OfficialName,
                    oi.ProductVariant.Product.Brand.LogoUrl
                })
                .Select(g => new
                {
                    BrandId = g.Key.BrandId,
                    BrandName = g.Key.OfficialName,
                    Logo = g.Key.LogoUrl,
                    TotalSales = g.Sum(x => x.Quantity * x.PriceAtTimeOfPurchase)
                })
                .OrderByDescending(x => x.TotalSales)
                .Take(10)
                .ToListAsync();

            return Ok(topBrands);
        }
    }
}
