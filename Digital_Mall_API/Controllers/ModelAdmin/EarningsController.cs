using Digital_Mall_API.Models.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Digital_Mall_API.Controllers.ModelAdmin
{
    [ApiController]
    [Route("Model/[controller]")]
    public class EarningsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EarningsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("Summary")]
        public async Task<ActionResult> GetEarningsSummary()
        {
            var modelId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(modelId))
                return Unauthorized("Model not authenticated");

            var query = _context.ReelCommissions
                .Where(rc => rc.FashionModelId == modelId)
                .AsQueryable();

            

            var commissions = await query.ToListAsync();

            var totalCommissions = commissions.Sum(rc => rc.CommissionAmount);
            var pendingAmounts = commissions.Where(rc => rc.Status == "Pending").Sum(rc => rc.CommissionAmount);
            var paidAmounts = commissions.Where(rc => rc.Status == "Paid").Sum(rc => rc.CommissionAmount);

            return Ok(new
            {
                TotalCommissions = Math.Round(totalCommissions, 2),
                PendingAmounts = Math.Round(pendingAmounts, 2),
                PaidAmounts = Math.Round(paidAmounts, 2),
               
            });
        }

        [HttpGet("CommissionReports")]
        public async Task<ActionResult> GetCommissionReports(
            [FromQuery] int? year = null,
            [FromQuery] int? month = null)
        {
            var modelId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(modelId))
                return Unauthorized("Model not authenticated");

            var query = _context.ReelCommissions
                .Include(rc => rc.Product)
                .Include(rc => rc.Brand)
                .Include(rc => rc.Reel)
                .Where(rc => rc.FashionModelId == modelId)
                .AsQueryable();

            if (year.HasValue)
            {
                query = query.Where(rc => rc.CreatedAt.Year == year.Value);
            }

            if (month.HasValue && year.HasValue)
            {
                query = query.Where(rc => rc.CreatedAt.Month == month.Value);
            }

           

            var reports = await query
                .OrderByDescending(rc => rc.CreatedAt)
                .Select(rc => new
                {
                    Id = rc.Id,
                    SubmissionDate = rc.CreatedAt.ToString("yyyy-MM-dd"),
                    Type = "Reel",
                    Amount = Math.Round(rc.CommissionAmount, 2) + " LE",
                    ProductName = rc.Product.Name,
                    BrandName = rc.Brand.OfficialName,
                    Status = rc.Status
                })
                .ToListAsync();

            return Ok(reports);
        }
    }
}
