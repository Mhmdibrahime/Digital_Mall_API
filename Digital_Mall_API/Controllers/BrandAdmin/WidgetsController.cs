using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.DTOs.BrandAdminDTOs.WidgetsDTOs;
using Digital_Mall_API.Models.Entities.Product_Catalog;
using Digital_Mall_API.Models.Entities.Promotions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Digital_Mall_API.Controllers.BrandAdmin
{
    [Route("Brand/[controller]")]
    [ApiController]
    public class WidgetsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public WidgetsController(AppDbContext context)
        {
            _context = context;
        }

        private string GetCurrentBrandId()
        {
            var brandId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return brandId;
        }

        [HttpGet("KPIs")]
        public async Task<ActionResult<KPIsDto>> GetKPIs()
        {
            var brandId = GetCurrentBrandId();
            if (string.IsNullOrEmpty(brandId))
            {
                return Unauthorized("Brand not authenticated.");
            }

            var kpis = new KPIsDto
            {
                TotalProducts = await GetTotalProductsData(brandId),
                TotalOrders = await GetTotalOrdersData(brandId),
                TotalRevenue = await GetTotalRevenueData(brandId),
                ActiveDiscounts = await GetActiveDiscountsData(brandId),
                AverageOrderPrice = await GetAverageOrderPriceData(brandId)
            };

            return Ok(kpis);
        }
        [HttpGet("MonthlySales/{year}")]
        public async Task<ActionResult<List<MonthlySalesDto>>> GetMonthlySales(int year)
        {
            var brandId = GetCurrentBrandId();
            if (string.IsNullOrEmpty(brandId))
            {
                return Unauthorized("Brand not authenticated.");
            }

            var currentYear = DateTime.UtcNow.Year;
            if (year < 2000 || year > currentYear + 1)
            {
                return BadRequest($"Year must be between 2000 and {currentYear}.");
            }

        
            var hasSalesInYear = await _context.OrderItems
                .AnyAsync(oi =>
                    oi.BrandId == brandId &&
                    oi.Order.OrderDate.Year == year &&
                    oi.Order.Status == "Delivered" &&
                    oi.Order.PaymentStatus == "Paid");

            if (!hasSalesInYear)
            {
                return Ok(new List<MonthlySalesDto>());
            }

            var monthlySales = new List<MonthlySalesDto>();
            var monthNames = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

            for (int month = 1; month <= 12; month++)
            {
                var monthStart = new DateTime(year, month, 1);
                var monthEnd = monthStart.AddMonths(1);

                var monthlyRevenue = await _context.OrderItems
                    .Where(oi =>
                        oi.BrandId == brandId &&
                        oi.Order.Status == "Delivered" &&
                        oi.Order.PaymentStatus == "Paid" &&
                        oi.Order.OrderDate >= monthStart &&
                        oi.Order.OrderDate < monthEnd)
                    .SumAsync(oi => (decimal?)(oi.Quantity * oi.PriceAtTimeOfPurchase)) ?? 0;

                monthlySales.Add(new MonthlySalesDto
                {
                    Month = monthNames[month - 1],
                    Sales = monthlyRevenue
                });
            }

            return Ok(monthlySales);
        }

        private async Task<TotalProductsDto> GetTotalProductsData(string brandId)
        {
            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;
            var lastMonth = currentMonth == 1 ? 12 : currentMonth - 1;
            var lastMonthYear = currentMonth == 1 ? currentYear - 1 : currentYear;

            var totalProducts = await _context.Products
                .Where(p => p.BrandId == brandId && p.IsActive)
                .CountAsync();

            var lastMonthProducts = await _context.Products
                .Where(p => p.BrandId == brandId &&
                           p.IsActive &&
                           p.CreatedAt.Month == lastMonth &&
                           p.CreatedAt.Year == lastMonthYear)
                .CountAsync();

            var currentMonthProducts = await _context.Products
                .Where(p => p.BrandId == brandId &&
                           p.IsActive &&
                           p.CreatedAt.Month == currentMonth &&
                           p.CreatedAt.Year == currentYear)
                .CountAsync();

            var percentageChange = lastMonthProducts > 0
                ? ((currentMonthProducts - lastMonthProducts) / (decimal)lastMonthProducts) * 100
                : (currentMonthProducts > 0 ? 100 : 0);

            return new TotalProductsDto
            {
                Count = totalProducts,
                PercentageChange = Math.Round(percentageChange, 1),
                Trend = percentageChange >= 0 ? "increase" : "decrease"
            };
        }

        private async Task<TotalOrdersDto> GetTotalOrdersData(string brandId)
        {
            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;
            var lastMonth = currentMonth == 1 ? 12 : currentMonth - 1;
            var lastMonthYear = currentMonth == 1 ? currentYear - 1 : currentYear;

         
            var totalOrders = await _context.OrderItems
                .Where(oi => oi.BrandId == brandId)
                .Select(oi => oi.OrderId)
                .Distinct()
                .CountAsync();

            var lastMonthOrders = await _context.OrderItems
                .Where(oi =>
                    oi.BrandId == brandId &&
                    oi.Order.OrderDate.Month == lastMonth &&
                    oi.Order.OrderDate.Year == lastMonthYear)
                .Select(oi => oi.OrderId)
                .Distinct()
                .CountAsync();

            var currentMonthOrders = await _context.OrderItems
                .Where(oi =>
                    oi.BrandId == brandId &&
                    oi.Order.OrderDate.Month == currentMonth &&
                    oi.Order.OrderDate.Year == currentYear)
                .Select(oi => oi.OrderId)
                .Distinct()
                .CountAsync();

            var percentageChange = lastMonthOrders > 0
                ? ((currentMonthOrders - lastMonthOrders) / (decimal)lastMonthOrders) * 100
                : (currentMonthOrders > 0 ? 100 : 0);

            return new TotalOrdersDto
            {
                Count = totalOrders,
                PercentageChange = Math.Round(percentageChange, 1),
                Trend = percentageChange >= 0 ? "increase" : "decrease"
            };
        }


        private async Task<TotalRevenueDto> GetTotalRevenueData(string brandId)
        {
            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;
            var lastMonth = currentMonth == 1 ? 12 : currentMonth - 1;
            var lastMonthYear = currentMonth == 1 ? currentYear - 1 : currentYear;

            var totalRevenue = await _context.OrderItems
                .Where(oi =>
                    oi.BrandId == brandId &&
                    oi.Order.Status == "Delivered" &&
                    oi.Order.PaymentStatus == "Paid")
                .SumAsync(oi => oi.Quantity * oi.PriceAtTimeOfPurchase);

            var lastMonthRevenue = await _context.OrderItems
                .Where(oi =>
                    oi.BrandId == brandId &&
                    oi.Order.Status == "Delivered" &&
                    oi.Order.PaymentStatus == "Paid" &&
                    oi.Order.OrderDate.Month == lastMonth &&
                    oi.Order.OrderDate.Year == lastMonthYear)
                .SumAsync(oi => oi.Quantity * oi.PriceAtTimeOfPurchase);

            var currentMonthRevenue = await _context.OrderItems
                .Where(oi =>
                    oi.BrandId == brandId &&
                    oi.Order.Status == "Delivered" &&
                    oi.Order.PaymentStatus == "Paid" &&
                    oi.Order.OrderDate.Month == currentMonth &&
                    oi.Order.OrderDate.Year == currentYear)
                .SumAsync(oi => oi.Quantity * oi.PriceAtTimeOfPurchase);

            var percentageChange = lastMonthRevenue > 0
                ? ((currentMonthRevenue - lastMonthRevenue) / lastMonthRevenue) * 100
                : (currentMonthRevenue > 0 ? 100 : 0);

            return new TotalRevenueDto
            {
                Amount = totalRevenue,
                PercentageChange = Math.Round(percentageChange, 1),
                Trend = percentageChange >= 0 ? "increase" : "decrease",
                Currency = "LE"
            };
        }


        private async Task<ActiveDiscountsDto> GetActiveDiscountsData(string brandId)
        {
            var activeDiscountsCount = await _context.Products
                .Where(p => p.BrandId == brandId &&
                           p.IsActive &&
                           p.DiscountId != null &&
                           p.Discount.Status == "Active")
                .Select(p => p.DiscountId)
                .Distinct()
                .CountAsync();

            return new ActiveDiscountsDto
            {
                Count = activeDiscountsCount,
                Description = "discount" + (activeDiscountsCount > 1 ? "s" : "")
            };
        }

        private async Task<AverageOrderPriceDto> GetAverageOrderPriceData(string brandId)
        {
            var orderRevenues = await _context.OrderItems
                .Where(oi =>
                    oi.BrandId == brandId &&
                    oi.Order.Status == "Delivered" &&
                    oi.Order.PaymentStatus == "Paid")
                .GroupBy(oi => oi.OrderId)
                .Select(g => g.Sum(x => x.Quantity * x.PriceAtTimeOfPurchase))
                .ToListAsync();

            var averageOrderPrice = orderRevenues.Any() ? orderRevenues.Average() : 0;

            return new AverageOrderPriceDto
            {
                Amount = Math.Round(averageOrderPrice, 2),
                Currency = "LE",
                Description = "per order"
            };
        }

    }
}