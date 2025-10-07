using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.Entities.Financials;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Digital_Mall_API.Controllers.Model
{
    [Route("Model/[controller]")]
    [ApiController]
    public class WidgetsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public WidgetsController(AppDbContext context)
        {
            _context = context;
        }

        private string GetCurrentModelId()
        {
            var modelId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return modelId;
        }

        [HttpGet("DashboardStats")]
        public async Task<ActionResult<DashboardStatsDto>> GetDashboardStats()
        {
            var modelId = GetCurrentModelId();
            if (string.IsNullOrEmpty(modelId))
            {
                return Unauthorized("Model not authenticated.");
            }

            var reelsStats = await GetReelsStatistics(modelId);

            var financialStats = await GetFinancialStatistics(modelId);

            return new DashboardStatsDto
            {
                ReelsStatistics = reelsStats,
                FinancialStatistics = financialStats
            };
        }

        [HttpGet("ReelsPerformanceChart")]
        public async Task<ActionResult<ReelsPerformanceChartDto>> GetReelsPerformanceChart(
            [FromQuery] int? year = null)
        {
            var modelId = GetCurrentModelId();
            if (string.IsNullOrEmpty(modelId))
            {
                return Unauthorized("Model not authenticated.");
            }

            var targetYear = year ?? DateTime.UtcNow.Year;
            var chartData = await GetPerformanceChartData(modelId, targetYear, "all");

            return chartData;
        }

        [HttpGet("ViewsPerformanceChart")]
        public async Task<ActionResult<PerformanceChartDto>> GetViewsPerformanceChart(
            [FromQuery] int? year = null)
        {
            var modelId = GetCurrentModelId();
            if (string.IsNullOrEmpty(modelId))
            {
                return Unauthorized("Model not authenticated.");
            }

            var targetYear = year ?? DateTime.UtcNow.Year;
            var chartData = await GetPerformanceChartData(modelId, targetYear, "views");

            return new PerformanceChartDto
            {
                Year = chartData.Year,
                MonthlyStats = chartData.MonthlyStats,
                Total = chartData.TotalViews,
                Type = "Views",
                Description = "Total views performance across all reels"
            };
        }

        [HttpGet("LikesPerformanceChart")]
        public async Task<ActionResult<PerformanceChartDto>> GetLikesPerformanceChart(
            [FromQuery] int? year = null)
        {
            var modelId = GetCurrentModelId();
            if (string.IsNullOrEmpty(modelId))
            {
                return Unauthorized("Model not authenticated.");
            }

            var targetYear = year ?? DateTime.UtcNow.Year;
            var chartData = await GetPerformanceChartData(modelId, targetYear, "likes");

            return new PerformanceChartDto
            {
                Year = chartData.Year,
                MonthlyStats = chartData.MonthlyStats,
                Total = chartData.TotalLikes,
                Type = "Likes",
                Description = "Total likes performance across all reels"
            };
        }

        [HttpGet("SharesPerformanceChart")]
        public async Task<ActionResult<PerformanceChartDto>> GetSharesPerformanceChart(
            [FromQuery] int? year = null)
        {
            var modelId = GetCurrentModelId();
            if (string.IsNullOrEmpty(modelId))
            {
                return Unauthorized("Model not authenticated.");
            }

            var targetYear = year ?? DateTime.UtcNow.Year;
            var chartData = await GetPerformanceChartData(modelId, targetYear, "shares");

            return new PerformanceChartDto
            {
                Year = chartData.Year,
                MonthlyStats = chartData.MonthlyStats,
                Total = chartData.TotalShares,
                Type = "Shares",
                Description = "Total shares performance across all reels"
            };
        }

        [HttpGet("AvailableYears")]
        public async Task<ActionResult<List<int>>> GetAvailableYears()
        {
            var modelId = GetCurrentModelId();
            if (string.IsNullOrEmpty(modelId))
            {
                return Unauthorized("Model not authenticated.");
            }

            var years = await _context.Reels
                .Where(r => r.PostedByUserId == modelId && r.PostedByUserType == "FashionModel")
                .Select(r => r.PostedDate.Year)
                .Distinct()
                .OrderByDescending(year => year)
                .ToListAsync();

            return years;
        }

        
        private async Task<ReelsPerformanceChartDto> GetPerformanceChartData(string modelId, int year, string type)
        {
            var reels = await _context.Reels
                .Where(r => r.PostedByUserId == modelId &&
                           r.PostedByUserType == "FashionModel" &&
                           r.PostedDate.Year == year)
                .ToListAsync();

            var monthlyStats = reels
                .GroupBy(r => r.PostedDate.Month)
                .Select(g => new MonthlyReelStat
                {
                    Month = g.Key,
                    MonthName = GetMonthName(g.Key),
                    Likes = g.Sum(r => r.LikesCount),
                    Shares = g.Sum(r => r.SharesCount),
                    Views = g.Sum(r => r.LikesCount) * 4,
                    ReelsCount = g.Count(),
                    AverageEngagement = g.Average(r => (r.LikesCount + r.SharesCount) / (r.LikesCount * 4.0)) * 100 // Engagement rate
                })
                .OrderBy(stat => stat.Month)
                .ToList();

            var allMonthsStats = Enumerable.Range(1, 12)
                .Select(month => new MonthlyReelStat
                {
                    Month = month,
                    MonthName = GetMonthName(month),
                    Likes = monthlyStats.FirstOrDefault(ms => ms.Month == month)?.Likes ?? 0,
                    Shares = monthlyStats.FirstOrDefault(ms => ms.Month == month)?.Shares ?? 0,
                    Views = monthlyStats.FirstOrDefault(ms => ms.Month == month)?.Views ?? 0,
                    ReelsCount = monthlyStats.FirstOrDefault(ms => ms.Month == month)?.ReelsCount ?? 0,
                    AverageEngagement = monthlyStats.FirstOrDefault(ms => ms.Month == month)?.AverageEngagement ?? 0
                })
                .ToList();

            return new ReelsPerformanceChartDto
            {
                Year = year,
                MonthlyStats = allMonthsStats,
                TotalLikes = allMonthsStats.Sum(ms => ms.Likes),
                TotalShares = allMonthsStats.Sum(ms => ms.Shares),
                TotalViews = allMonthsStats.Sum(ms => ms.Views),
                TotalReels = allMonthsStats.Sum(ms => ms.ReelsCount),
                AverageEngagement = allMonthsStats.Average(ms => ms.AverageEngagement)
            };
        }

        private async Task<ReelsStatisticsDto> GetReelsStatistics(string modelId)
        {
            var reels = await _context.Reels
                .Where(r => r.PostedByUserId == modelId && r.PostedByUserType == "FashionModel")
                .ToListAsync();

            var totalReels = reels.Count;
            var totalViews = reels.Sum(r => r.LikesCount) * 4;
            var totalLikes = reels.Sum(r => r.LikesCount);
            var totalShares = reels.Sum(r => r.SharesCount);

            return new ReelsStatisticsDto
            {
                TotalReelsUploaded = totalReels,
                TotalViews = totalViews,
                TotalLikes = totalLikes,
                TotalShares = totalShares
            };
        }

        private async Task<FinancialStatisticsDto> GetFinancialStatistics(string modelId)
        {
            var commissions = await _context.ReelCommissions
                .Where(rc => rc.FashionModelId == modelId)
                .ToListAsync();

            var totalCommissions = commissions.Sum(rc => rc.CommissionAmount);
            var pendingCommissions = commissions.Where(rc => rc.Status == "Pending").Sum(rc => rc.CommissionAmount);
            var paidCommissions = commissions.Where(rc => rc.Status == "Paid").Sum(rc => rc.CommissionAmount);

            var totalCommissionsCount = commissions.Count;
            var pendingCommissionsCount = commissions.Count(rc => rc.Status == "Pending");
            var paidCommissionsCount = commissions.Count(rc => rc.Status == "Paid");

            return new FinancialStatisticsDto
            {
                TotalCommissions = new CommissionStat
                {
                    Amount = totalCommissions,
                    Count = totalCommissionsCount,
                    Description = $"You have {totalCommissionsCount} total commissions"
                },
                PendingCommissions = new CommissionStat
                {
                    Amount = pendingCommissions,
                    Count = pendingCommissionsCount,
                    Description = $"{pendingCommissionsCount} of them pending"
                },
                PaidCommissions = new CommissionStat
                {
                    Amount = paidCommissions,
                    Count = paidCommissionsCount,
                    Description = $"{paidCommissionsCount} of them paid"
                }
            };
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
    }

    public class DashboardStatsDto
    {
        public ReelsStatisticsDto ReelsStatistics { get; set; }
        public FinancialStatisticsDto FinancialStatistics { get; set; }
    }

    public class ReelsStatisticsDto
    {
        public int TotalReelsUploaded { get; set; }
        public int TotalViews { get; set; }
        public int TotalLikes { get; set; }
        public int TotalShares { get; set; }
    }

    public class FinancialStatisticsDto
    {
        public CommissionStat TotalCommissions { get; set; }
        public CommissionStat PendingCommissions { get; set; }
        public CommissionStat PaidCommissions { get; set; }
    }

    public class CommissionStat
    {
        public decimal Amount { get; set; }
        public int Count { get; set; }
        public string Description { get; set; }
    }

    public class ReelsPerformanceChartDto
    {
        public int Year { get; set; }
        public List<MonthlyReelStat> MonthlyStats { get; set; }
        public int TotalLikes { get; set; }
        public int TotalShares { get; set; }
        public int TotalViews { get; set; }
        public int TotalReels { get; set; }
        public double AverageEngagement { get; set; }
    }

    public class PerformanceChartDto
    {
        public int Year { get; set; }
        public List<MonthlyReelStat> MonthlyStats { get; set; }
        public int Total { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
    }

    public class MonthlyReelStat
    {
        public int Month { get; set; }
        public string MonthName { get; set; }
        public int Likes { get; set; }
        public int Shares { get; set; }
        public int Views { get; set; }
        public int ReelsCount { get; set; }
        public double AverageEngagement { get; set; }
    }

    public class TopReelDto
    {
        public int Id { get; set; }
        public string Caption { get; set; }
        public string ThumbnailUrl { get; set; }
        public DateTime PostedDate { get; set; }
        public int Likes { get; set; }
        public int Shares { get; set; }
        public int Views { get; set; }
        public int Duration { get; set; }
    }
}