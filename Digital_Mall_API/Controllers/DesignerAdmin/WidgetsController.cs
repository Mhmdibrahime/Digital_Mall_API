using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.DTOs.DesignerAdminDTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Digital_Mall_API.Controllers.DesignerAdmin
{
    [Route("Designer/cards/[controller]")]
    [ApiController]
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

    }
}
