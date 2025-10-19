using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.DTOs.DesignerAdminDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Digital_Mall_API.Controllers.DesignerAdmin
{
    [Route("Designer/cards/[controller]")]
    [ApiController]
    [Authorize(Roles = "Designer")]

    public class WidgetsController : ControllerBase
    {
        private readonly AppDbContext context;

        public WidgetsController(AppDbContext context)
        {
            this.context = context;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardStats()
        {
            // Your existing dashboard stats code...
            var now = DateTime.UtcNow;
            var currentMonthStart = new DateTime(now.Year, now.Month, 1);
            var previousMonthStart = currentMonthStart.AddMonths(-1);
            var previousMonthEnd = currentMonthStart.AddDays(-1);

            var totalRequestsCurrent = await context.TshirtDesignOrders
                .CountAsync(o => o.RequestDate >= currentMonthStart && o.RequestDate <= now);

            var pendingRequestsCurrent = await context.TshirtDesignOrders
                .CountAsync(o => o.Status == "Pending" && o.RequestDate >= currentMonthStart && o.RequestDate <= now);

            var completedDesignsCurrent = await context.TshirtDesignOrders
                .CountAsync(o => o.Status == "Completed" && o.RequestDate >= currentMonthStart && o.RequestDate <= now);

            var earningsCurrent = await context.TshirtDesignOrders
                .Where(o => o.IsPaid && o.RequestDate >= currentMonthStart && o.RequestDate <= now)
                .SumAsync(o => (decimal?)o.FinalPrice) ?? 0;

            var totalRequestsPrev = await context.TshirtDesignOrders
                .CountAsync(o => o.RequestDate >= previousMonthStart && o.RequestDate <= previousMonthEnd);

            var pendingRequestsPrev = await context.TshirtDesignOrders
                .CountAsync(o => o.Status == "Pending" && o.RequestDate >= previousMonthStart && o.RequestDate <= previousMonthEnd);

            var completedDesignsPrev = await context.TshirtDesignOrders
                .CountAsync(o => o.Status == "Completed" && o.RequestDate >= previousMonthStart && o.RequestDate <= previousMonthEnd);

            var earningsPrev = await context.TshirtDesignOrders
                .Where(o => o.IsPaid && o.RequestDate >= previousMonthStart && o.RequestDate <= previousMonthEnd)
                .SumAsync(o => (decimal?)o.FinalPrice) ?? 0;

            double CalcPercentageChange(double current, double previous)
            {
                if (previous == 0 && current > 0) return 100;
                if (previous == 0 && current == 0) return 0;
                return (current - previous) / previous * 100;
            }

            var stats = new DashboardStatsDto
            {
                TotalRequests = totalRequestsCurrent,
                TotalRequestsChange = CalcPercentageChange(totalRequestsCurrent, totalRequestsPrev),

                PendingRequests = pendingRequestsCurrent,
                PendingRequestsChange = CalcPercentageChange(pendingRequestsCurrent, pendingRequestsPrev),

                CompletedDesigns = completedDesignsCurrent,
                CompletedDesignsChange = CalcPercentageChange(completedDesignsCurrent, completedDesignsPrev),

                Earnings = earningsCurrent,
                EarningsChange = CalcPercentageChange((double)earningsCurrent, (double)earningsPrev)
            };

            return Ok(stats);
        }

        [HttpGet("earnings-chart")]
        public async Task<IActionResult> GetEarningsChartData([FromQuery] int year = 2025)
        {
            var earningsData = await context.TshirtDesignOrders
                .Where(o => o.RequestDate.Year == year && o.IsPaid)
                .GroupBy(o => o.RequestDate.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Earnings = g.Sum(o => (decimal?)o.FinalPrice) ?? 0
                })
                .OrderBy(x => x.Month)
                .ToListAsync();

            // Fill in missing months with zero earnings
            var allMonthsData = Enumerable.Range(1, 12)
                .Select(month => new
                {
                    Month = month,
                    Earnings = earningsData.FirstOrDefault(e => e.Month == month)?.Earnings ?? 0
                })
                .ToList();

            var result = new
            {
                Year = year,
                MonthlyEarnings = allMonthsData.Select(e => new
                {
                    MonthName = new DateTime(year, e.Month, 1).ToString("MMM"),
                    Earnings = e.Earnings
                })
            };

            return Ok(result);
        }

        [HttpGet("requests-chart")]
        public async Task<IActionResult> GetRequestsChartData([FromQuery] int year = 2025)
        {
            var requestsData = await context.TshirtDesignOrders
                .Where(o => o.RequestDate.Year == year)
                .GroupBy(o => new { o.RequestDate.Month, o.Status })
                .Select(g => new
                {
                    Month = g.Key.Month,
                    Status = g.Key.Status,
                    Count = g.Count()
                })
                .ToListAsync();

            // Fill in missing months with zero counts for all statuses
            var allMonthsData = Enumerable.Range(1, 12)
                .SelectMany(month => new[]
                {
                    new { Month = month, Status = "Completed", Count = 0 },
                    new { Month = month, Status = "In Progress", Count = 0 },
                    new { Month = month, Status = "Pending", Count = 0 },
                    new { Month = month, Status = "Rejected", Count = 0 }
                })
                .ToList();

            // Merge actual data with the zero-filled template
            var mergedData = allMonthsData
                .GroupJoin(requestsData,
                    template => new { template.Month, template.Status },
                    actual => new { actual.Month, actual.Status },
                    (template, actuals) => new { template, actuals })
                .SelectMany(x => x.actuals.DefaultIfEmpty(),
                    (x, actual) => new
                    {
                        x.template.Month,
                        x.template.Status,
                        Count = actual?.Count ?? 0
                    })
                .GroupBy(x => x.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    MonthName = new DateTime(year, g.Key, 1).ToString("MMM"),
                    Completed = g.FirstOrDefault(x => x.Status == "Completed")?.Count ?? 0,
                    InProgress = g.FirstOrDefault(x => x.Status == "In Progress")?.Count ?? 0,
                    Pending = g.FirstOrDefault(x => x.Status == "Pending")?.Count ?? 0,
                    Rejected = g.FirstOrDefault(x => x.Status == "Rejected")?.Count ?? 0
                })
                .OrderBy(x => x.Month)
                .ToList();

            var result = new
            {
                Year = year,
                MonthlyRequests = mergedData
            };

            return Ok(result);
        }
    }
}