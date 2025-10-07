using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.DTOs.SuperAdminDTOs.Reports_AnalyticsDTOs;
using Digital_Mall_API.Models.Entities.Reels___Content;
using Digital_Mall_API.Models.Entities.User___Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Controllers.SuperAdmin
{
    [Route("Super/[controller]")]
    [ApiController]
    public class ReportsAndAnalyticsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReportsAndAnalyticsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("ModelsSummary")]
        public async Task<IActionResult> GetModelsReelsSummary(
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _context.FashionModels
                .Include(m => m.Reels)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(m =>
                    m.Name.Contains(search) ||
                    m.Bio.Contains(search));
            }

            var totalCount = await query.CountAsync();

            var models = await query
                .OrderByDescending(m => m.Reels.Sum(r => r.LikesCount))
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new ModelReelsSummaryDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    JoinDate = m.CreatedAt,
                    TotalReels = m.Reels.Count,
                    TotalLikes = m.Reels.Sum(r => r.LikesCount),
                    TotalShares = m.Reels.Sum(r => r.SharesCount)

                })
                .ToListAsync();

            return Ok(new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Models = models
            });
        }

        [HttpGet("ModelReels/{modelId}")]
        public async Task<IActionResult> GetModelReels(string modelId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var model = await _context.FashionModels
                .FirstOrDefaultAsync(m => m.Id == modelId);

            if (model == null)
            {
                return NotFound("Model not found");
            }

            var reelsQuery = _context.Reels
                .Where(r => r.PostedByUserId == modelId)
                .OrderByDescending(r => r.PostedDate);

            var totalCount = await reelsQuery.CountAsync();

            var reels = await reelsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new ReelDetailDto
                {
                    Id = r.Id,
                    Name = r.Caption ?? "Untitled Reel",
                    PublishDate = r.PostedDate,
                    Likes = r.LikesCount,
                    Shares = r.SharesCount

                })
                .ToListAsync();

            return Ok(new
            {
                ModelName = model.Name,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Reels = reels
            });
        }

        [HttpGet("BrandSales")]
        public async Task<IActionResult> GetBrandSalesReport(
       [FromQuery] int? year = 2025,
       [FromQuery] string? brandId = null)
        {
            
            var availableYears = await _context.OrderItems
                .Where(oi => oi.Order.OrderDate.Year >= 2025)
                .Select(oi => oi.Order.OrderDate.Year)
                .Distinct()
                .OrderBy(y => y)
                .ToListAsync();

            var availableBrands = await _context.Brands
                .Where(b => b.Status == "Active")
                .Select(b => new { b.Id, b.OfficialName })
                .ToListAsync();

           
            var orderItemsQuery = _context.OrderItems
                .Include(oi => oi.Brand)
                .Include(oi => oi.Order)
                .Where(oi =>
                    oi.Order.Status != "Cancelled" &&
                    oi.Order.PaymentStatus == "Paid");

            if (year.HasValue)
            {
                orderItemsQuery = orderItemsQuery.Where(oi => oi.Order.OrderDate.Year == year.Value);
            }

            if (!string.IsNullOrEmpty(brandId))
            {
                orderItemsQuery = orderItemsQuery.Where(oi => oi.BrandId == brandId);
            }

            var salesData = await orderItemsQuery
                .GroupBy(oi => new { Year = oi.Order.OrderDate.Year, BrandId = oi.BrandId })
                .Select(g => new BrandSalesDto
                {
                    Year = g.Key.Year,
                    BrandId = g.Key.BrandId,
                    BrandName = g.First().Brand.OfficialName,
                    Sales = g.Sum(x => x.Quantity * x.PriceAtTimeOfPurchase)
                })
                .OrderBy(s => s.Year)
                .ThenBy(s => s.BrandName)
                .ToListAsync();

            var result = salesData
                .GroupBy(s => s.Year)
                .Select(g => new
                {
                    Year = g.Key,
                    Brands = g.Select(b => new
                    {
                        b.BrandId,
                        b.BrandName,
                        b.Sales
                    }).ToList(),
                    TotalSales = g.Sum(b => b.Sales)
                })
                .OrderBy(g => g.Year)
                .ToList();

            return Ok(new
            {
                AvailableYears = availableYears,
                AvailableBrands = availableBrands,
                SelectedYear = year,
                SelectedBrandId = brandId,
                Data = result
            });
        }

        [HttpGet("UserGrowth")]
        public async Task<IActionResult> GetUserGrowthReport(
            [FromQuery] int? year = null)
        {
            var targetYear = year ?? DateTime.UtcNow.Year;

            var customerQuery = _context.Customers.Where(c => c.CreatedAt.Year == targetYear);
            var brandQuery = _context.Brands.Where(b => b.CreatedAt.Year == targetYear && b.Status == "Active");
            var modelQuery = _context.FashionModels.Where(m => m.CreatedAt.Year == targetYear && m.Status == "Active");


            var monthlyGrowth = new List<UserGrowthMonthlyDto>();

            for (int month = 1; month <= 12; month++)
            {
                var monthData = new UserGrowthMonthlyDto
                {
                    Year = targetYear,
                    Month = month,
                    MonthName = new DateTime(targetYear, month, 1).ToString("MMM"),
                    Customers = 0,
                    Brands = 0,
                    Models = 0
                };
                monthlyGrowth.Add(monthData);
            }

            var customerGrowth = await customerQuery
                .GroupBy(c => c.CreatedAt.Month)
                .Select(g => new { Month = g.Key, Count = g.Count() })
                .ToListAsync();

            var brandGrowth = await brandQuery
                .GroupBy(b => b.CreatedAt.Month)
                .Select(g => new { Month = g.Key, Count = g.Count() })
                .ToListAsync();
            var modelGrowth = await modelQuery
                .GroupBy(m => m.CreatedAt.Month)
                .Select(g => new { Month = g.Key, Count = g.Count() })
                .ToListAsync();

            foreach (var customerData in customerGrowth)
            {
                var monthData = monthlyGrowth.FirstOrDefault(m => m.Month == customerData.Month);
                if (monthData != null)
                    monthData.Customers = customerData.Count;
            }

            foreach (var brandData in brandGrowth)
            {
                var monthData = monthlyGrowth.FirstOrDefault(m => m.Month == brandData.Month);
                if (monthData != null)
                    monthData.Brands = brandData.Count;
            }

            foreach (var modelData in modelGrowth)
            {
                var monthData = monthlyGrowth.FirstOrDefault(m => m.Month == modelData.Month);
                if (monthData != null)
                    monthData.Models = modelData.Count;
            }

            var totalCustomers = monthlyGrowth.Sum(m => m.Customers);
            var totalBrands = monthlyGrowth.Sum(m => m.Brands);
            var totalModels = monthlyGrowth.Sum(m => m.Models);

            var availableYears = await GetAvailableUserYears();

            return Ok(new
            {
                Year = targetYear,
                AvailableYears = availableYears,
                
                Summary = new
                {
                    TotalCustomers = totalCustomers,
                    TotalBrands = totalBrands,
                    TotalModels = totalModels,
                    TotalUsers = totalCustomers + totalBrands + totalModels
                },
                MonthlyData = monthlyGrowth.OrderBy(m => m.Month).ToList()
            });
        }

        private async Task<List<int>> GetAvailableUserYears()
        {
            var customerYears = await _context.Customers
                .Select(c => c.CreatedAt.Year)
                .Distinct()
                .ToListAsync();

            var brandYears = await _context.Brands
                .Select(b => b.CreatedAt.Year)
                .Distinct()
                .ToListAsync();

            var modelYears = await _context.FashionModels
                .Select(m => m.CreatedAt.Year)
                .Distinct()
                .ToListAsync();

            return customerYears
                .Union(brandYears)
                .Union(modelYears)
                .OrderBy(y => y)
                .ToList();
        }
        [HttpGet("Revenue")]
        public async Task<IActionResult> GetRevenueReport([FromQuery] int? year = null)
        {
            var targetYear = year ?? DateTime.UtcNow.Year;

            var monthlyRevenue = await _context.Orders
                .Where(o => o.OrderDate.Year == targetYear && o.Status != "Cancelled" && o.PaymentStatus == "Paid")
                .GroupBy(o => o.OrderDate.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    MonthName = new DateTime(targetYear, g.Key, 1).ToString("MMM"),
                    Revenue = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count()
                })
                .OrderBy(r => r.Month)
                .ToListAsync();

            var completeMonthlyData = new List<object>();

            for (int month = 1; month <= 12; month++)
            {
                var existingData = monthlyRevenue.FirstOrDefault(m => m.Month == month);
                if (existingData != null)
                {
                    completeMonthlyData.Add(existingData);
                }
                else
                {
                    completeMonthlyData.Add(new
                    {
                        Month = month,
                        MonthName = new DateTime(targetYear, month, 1).ToString("MMM"),
                        Revenue = 0m,
                        OrderCount = 0
                    });
                }
            }

            return Ok(new
            {
                Year = targetYear,
                MonthlyData = completeMonthlyData
            });
        }
    }
}