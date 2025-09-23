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
            var availableYears = await _context.Orders
                .Where(o => o.OrderDate.Year >= 2025) 
                .Select(o => o.OrderDate.Year)
                .Distinct()
                .OrderBy(y => y)
                .ToListAsync();

           
            var availableBrands = await _context.Brands
                .Where(b => b.Status == "Active")
                .Select(b => new { b.Id, b.OfficialName })
                .ToListAsync();

            var ordersQuery = _context.Orders
                .Include(o => o.Brand)
                .Where(o => o.Status != "Cancelled"); 

            if (year.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.OrderDate.Year == year.Value);
            }

            if (!string.IsNullOrEmpty(brandId))
            {
                ordersQuery = ordersQuery.Where(o => o.BrandId == brandId);
            }

            var salesData = await ordersQuery
                .GroupBy(o => new { Year = o.OrderDate.Year, BrandId = o.BrandId })
                .Select(g => new BrandSalesDto
                {
                    Year = g.Key.Year,
                    BrandId = g.Key.BrandId,
                    BrandName = g.First().Brand.OfficialName,
                    Sales = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count()
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
                        BrandId = b.BrandId,
                        BrandName = b.BrandName,
                        Sales = b.Sales,
                        OrderCount = b.OrderCount
                    }).ToList(),
                    TotalSales = g.Sum(b => b.Sales),
                    TotalOrders = g.Sum(b => b.OrderCount)
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

        [HttpGet("BrandSalesChart/{year}")]
        public async Task<IActionResult> GetBrandSalesChartData(int year, [FromQuery] string chartType = "bar")
        {
            // Validate year exists in data
            var yearExists = await _context.Orders
                .AnyAsync(o => o.OrderDate.Year == year && o.Status != "Cancelled");

            if (!yearExists)
            {
                return NotFound($"No sales data found for year {year}");
            }

            // Get sales data for the specific year
            var chartData = await _context.Orders
                .Include(o => o.Brand)
                .Where(o => o.OrderDate.Year == year && o.Status != "Cancelled")
                .GroupBy(o => new { o.BrandId, o.Brand.OfficialName })
                .Select(g => new
                {
                    BrandId = g.Key.BrandId,
                    BrandName = g.Key.OfficialName,
                    Sales = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count()
                })
                .OrderByDescending(x => x.Sales)
                .ToListAsync();

            var response = new
            {
                Year = year,
                ChartType = chartType.ToLower(), // "bar" or "line"
                Data = chartData,
                TotalSales = chartData.Sum(d => d.Sales),
                TotalOrders = chartData.Sum(d => d.OrderCount),
                ChartTitle = $"{year} - {(chartType.ToLower() == "bar" ? "Brands Bar Chart" : "Brands Line Chart")}"
            };

            return Ok(response);
        }

        [HttpGet("BrandSalesSummary")]
        public async Task<IActionResult> GetBrandSalesSummary()
        {
            // Get available years from orders
            var availableYears = await _context.Orders
                .Where(o => o.Status != "Cancelled")
                .Select(o => o.OrderDate.Year)
                .Distinct()
                .OrderBy(y => y)
                .ToListAsync();

            // Get active brands
            var availableBrands = await _context.Brands
                .Where(b => b.Status == "Active")
                .Select(b => new { b.Id, b.OfficialName })
                .OrderBy(b => b.OfficialName)
                .ToListAsync();

            // Get overall summary
            var totalSales = await _context.Orders
                .Where(o => o.Status != "Cancelled")
                .SumAsync(o => o.TotalAmount);

            var totalOrders = await _context.Orders
                .Where(o => o.Status != "Cancelled")
                .CountAsync();

            var totalBrands = availableBrands.Count;

            return Ok(new
            {
                AvailableYears = availableYears,
                AvailableBrands = availableBrands,
                DefaultYear = availableYears.LastOrDefault(), // Most recent year
                AllBrandsOption = "All Brands",
                Summary = new
                {
                    TotalSales = totalSales,
                    TotalOrders = totalOrders,
                    TotalBrands = totalBrands
                }
            });
        }

        [HttpGet("BrandSalesTrends")]
        public async Task<IActionResult> GetBrandSalesTrends([FromQuery] string? brandId = null)
        {
            // Get monthly sales trends for the last 2 years
            var startDate = DateTime.UtcNow.AddYears(-2);

            var trendsQuery = _context.Orders
                .Include(o => o.Brand)
                .Where(o => o.OrderDate >= startDate && o.Status != "Cancelled");

            if (!string.IsNullOrEmpty(brandId))
            {
                trendsQuery = trendsQuery.Where(o => o.BrandId == brandId);
            }

            var monthlyTrends = await trendsQuery
                .GroupBy(o => new {
                    Year = o.OrderDate.Year,
                    Month = o.OrderDate.Month,
                    BrandId = o.BrandId,
                    BrandName = o.Brand.OfficialName
                })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    BrandId = g.Key.BrandId,
                    BrandName = g.Key.BrandName,
                    Sales = g.Sum(o => o.TotalAmount),
                    Orders = g.Count(),
                    Period = new DateTime(g.Key.Year, g.Key.Month, 1)
                })
                .OrderBy(x => x.Period)
                .ThenBy(x => x.BrandName)
                .ToListAsync();

            return Ok(new
            {
                StartDate = startDate,
                EndDate = DateTime.UtcNow,
                BrandFilter = brandId,
                Trends = monthlyTrends
            });
        }






    }
}