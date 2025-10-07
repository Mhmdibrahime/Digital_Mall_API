using Digital_Mall_API.Models.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Digital_Mall_API.Controllers.SuperAdmin
{
    [Route("Super/[controller]")]
    [ApiController]
    public class WidgetsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public WidgetsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("KPIs")]
        public async Task<IActionResult> GetDashboardSummary()
        {
            var now = DateTime.Now;
            var firstDayCurrentMonth = new DateTime(now.Year, now.Month, 1);
            var firstDayPrevMonth = firstDayCurrentMonth.AddMonths(-1);
            var firstDayTwoMonthsAgo = firstDayCurrentMonth.AddMonths(-2);

            var currentRevenue = await _context.Orders
                .Where(o => o.OrderDate >= firstDayCurrentMonth &&
                           (o.Status == "completed" || o.Status == "delivered"))
                .SumAsync(o => o.TotalAmount);

            var previousRevenue = await _context.Orders
                .Where(o => o.OrderDate >= firstDayPrevMonth &&
                           o.OrderDate < firstDayCurrentMonth &&
                           (o.Status == "completed" || o.Status == "delivered"))
                .SumAsync(o => o.TotalAmount);

            var revenueChange = CalculatePercentageChange(currentRevenue, previousRevenue);

            var currentOrders = await _context.Orders
                .Where(o => o.OrderDate >= firstDayCurrentMonth)
                .CountAsync();

            var previousOrders = await _context.Orders
                .Where(o => o.OrderDate >= firstDayPrevMonth && o.OrderDate < firstDayCurrentMonth)
                .CountAsync();

            var ordersChange = CalculatePercentageChange(currentOrders, previousOrders);

            var currentUsers = await _context.Users
                .Where(u => u.CreatedAt >= firstDayCurrentMonth)
                .CountAsync();

            var previousUsers = await _context.Users
                .Where(u => u.CreatedAt >= firstDayPrevMonth && u.CreatedAt < firstDayCurrentMonth)
                .CountAsync();

            var usersChange = CalculatePercentageChange(currentUsers, previousUsers);

            var currentBrands = await _context.Brands
                .Where(b => b.CreatedAt >= firstDayCurrentMonth)
                .CountAsync();

            var previousBrands = await _context.Brands
                .Where(b => b.CreatedAt >= firstDayPrevMonth && b.CreatedAt < firstDayCurrentMonth)
                .CountAsync();

            var brandsChange = CalculatePercentageChange(currentBrands, previousBrands);

            var currentModels = await _context.FashionModels
                .Where(m => m.CreatedAt >= firstDayCurrentMonth)
                .CountAsync();

            var previousModels = await _context.FashionModels
                .Where(m => m.CreatedAt >= firstDayPrevMonth && m.CreatedAt < firstDayCurrentMonth)
                .CountAsync();

            var modelsChange = CalculatePercentageChange(currentModels, previousModels);

            return Ok(new
            {
                TotalRevenue = new
                {
                    Value = currentRevenue,
                    Change = revenueChange
                },
                TotalOrders = new
                {
                    Value = currentOrders,
                    Change = ordersChange
                },
                TotalUsers = new
                {
                    Value = currentUsers,
                    Change = usersChange
                },
                TotalBrands = new
                {
                    Value = currentBrands,
                    Change = brandsChange
                },
                TotalModels = new
                {
                    Value = currentModels,
                    Change = modelsChange
                }
            });
        }

        
        private decimal CalculatePercentageChange(decimal current, decimal previous)
        {
            if (previous == 0)
                return current > 0 ? 100 : 0;

            return ((current - previous) / previous) * 100;
        }

        [HttpGet("Charts/MonthlySales")]
        public async Task<IActionResult> GetMonthlySales()
        {
            var currentYear = DateTime.Now.Year;

            var monthlySales = await _context.Orders
                .AsNoTracking()
                .Where(o => o.OrderDate.Year == currentYear &&
                           (o.Status == "completed" || o.Status == "delivered"))
                .GroupBy(o => o.OrderDate.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    TotalSales = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count()
                })
                .ToListAsync();

            var allMonths = Enumerable.Range(1, 12).Select(month => new
            {
                Month = month,
                MonthName = GetMonthName(month),
                TotalSales = monthlySales.FirstOrDefault(m => m.Month == month)?.TotalSales ?? 0m,
                OrderCount = monthlySales.FirstOrDefault(m => m.Month == month)?.OrderCount ?? 0
            })
            .OrderBy(m => m.Month)
            .ToList();

            return Ok(new
            {
                CurrentYear = currentYear,
                MonthlyData = allMonths
            });
        }

        private string GetMonthName(int month)
        {
            return month switch
            {
                1 => "Jan",
                2 => "Feb",
                3 => "Mar",
                4 => "Apr",
                5 => "May",
                6 => "Jun",
                7 => "Jul",
                8 => "Aug",
                9 => "Sep",
                10 => "Oct",
                11 => "Nov",
                12 => "Dec",
                _ => "Unknown"
            };
        }
        
        [HttpGet("Charts/MonthlyUsers")]
        public async Task<IActionResult> GetMonthlyUsers()
        {
            var currentYear = DateTime.Now.Year;

            var monthlyUsers = await _context.Customers
                .AsNoTracking()
                .Where(u => u.CreatedAt.Year == currentYear)
                .GroupBy(u => u.CreatedAt.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    UserCount = g.Count()
                })
                .ToListAsync();

            var allMonths = Enumerable.Range(1, 12).Select(month => new
            {
                Month = month,
                MonthName = GetMonthName(month),
                UserCount = monthlyUsers.FirstOrDefault(m => m.Month == month)?.UserCount ?? 0
            })
            .OrderBy(m => m.Month)
            .ToList();

            return Ok(new
            {
                CurrentYear = currentYear,
                MonthlyData = allMonths
            });
        }

        [HttpGet("Charts/TopBrands")]
        public async Task<IActionResult> GetTopBrands()
        {
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

            var topBrands = await _context.Brands
                .AsNoTracking()
                .Select(b => new
                {
                    b.Id,
                    b.OfficialName,
                    b.LogoUrl,

                    TotalSales = _context.OrderItems
                        .Where(oi => oi.BrandId == b.Id &&
                                     (oi.Order.Status == "Completed" || oi.Order.Status == "Delivered"))
                        .Sum(oi => (decimal?)oi.PriceAtTimeOfPurchase * oi.Quantity) ?? 0m,

                    Last30DaysSales = _context.OrderItems
                        .Where(oi => oi.BrandId == b.Id &&
                                     (oi.Order.Status == "Completed" || oi.Order.Status == "Delivered") &&
                                     oi.Order.OrderDate >= thirtyDaysAgo)
                        .Sum(oi => (decimal?)oi.PriceAtTimeOfPurchase * oi.Quantity) ?? 0m,

                    
                    TotalOrders = _context.OrderItems
                        .Where(oi => oi.BrandId == b.Id)
                        .Select(oi => oi.OrderId)
                        .Distinct()
                        .Count(),

                  
                    RecentOrders = _context.OrderItems
                        .Where(oi => oi.BrandId == b.Id && oi.Order.OrderDate >= thirtyDaysAgo)
                        .Select(oi => oi.OrderId)
                        .Distinct()
                        .Count()
                })
                .OrderByDescending(b => b.TotalSales)
                .ThenByDescending(b => b.Last30DaysSales)
                .Take(5) 
                .ToListAsync();

            return Ok(topBrands);
        }
    }
}