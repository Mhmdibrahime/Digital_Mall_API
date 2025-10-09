using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.DTOs.BrandAdminDTOs.BrandPayoutsDTOs;
using Digital_Mall_API.Models.Entities.Financials;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Digital_Mall_API.Controllers.BrandAdmin
{
    [Route("Brand/[controller]")]
    [ApiController]
    public class BrandPayoutsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BrandPayoutsController(AppDbContext context)
        {
            _context = context;
        }

        private string GetCurrentBrandId()
        {
            var brandId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return brandId;
        }

        private async Task<Guid?> GetBrandUserIdAsync(string brandId)
        {
            var brandUser = await _context.Users.FirstOrDefaultAsync(u => u.Id.ToString() == brandId);
            return brandUser?.Id;
        }

        [HttpGet("earnings")]
        public async Task<ActionResult<FinancialEarningsDto>> GetFinancialEarnings()
        {
            var brandId = GetCurrentBrandId();
            if (string.IsNullOrEmpty(brandId))
            {
                return Unauthorized("Brand not authenticated.");
            }

            var totalRevenue = await _context.OrderItems
     .Where(oi =>
         oi.BrandId == brandId &&
         oi.Order.Status == "Deliverd" &&
         oi.Order.PaymentStatus == "Paid")
     .SumAsync(oi => oi.Quantity * oi.PriceAtTimeOfPurchase);

            var brandOrderItems = await _context.OrderItems
                .Include(oi => oi.Order)
                .Where(oi =>
                    oi.BrandId == brandId &&
                    oi.Order.Status == "Deliverd" &&
                    oi.Order.PaymentStatus == "Paid")
                .ToListAsync();

            if (!brandOrderItems.Any())
                return BadRequest("Brand hs no orders yet");


            var brand = await _context.Brands.FindAsync(brandId);
            var globalCommissionRate = await _context.GlobalCommission
                .Select(g => g.CommissionRate)
                .FirstOrDefaultAsync();

            var commissionRate = brand?.SpecificCommissionRate ?? globalCommissionRate;


            var platformCommissionDeductions = brandOrderItems
                .Sum(oi => oi.PriceAtTimeOfPurchase * oi.Quantity * (commissionRate / 100));


            var modelCommissionDeductions = await _context.ReelCommissions
                .Where(rc => rc.BrandId == brandId)
                .SumAsync(rc => rc.CommissionAmount);

            var brandUserId = await GetBrandUserIdAsync(brandId);

            
            var totalPaidOut = brandUserId.HasValue
                ? await _context.Payouts
                    .Where(p => p.PayeeUserId == brandUserId.Value && p.Status == "Paid")
                    .SumAsync(p => p.Amount)
                : 0;

           
            var pendingPayments = brandUserId.HasValue
                ? await _context.Payouts
                    .Where(p => p.PayeeUserId == brandUserId.Value &&
                                (p.Status == "Approved" || p.Status == "Pending"))
                    .SumAsync(p => p.Amount)
                : 0;

            

            var totalDeductions = platformCommissionDeductions + modelCommissionDeductions;
            var netEarnings = totalRevenue - totalDeductions;
            var availableForPayout = Math.Max(0, netEarnings - (totalPaidOut+pendingPayments));

           
            return new FinancialEarningsDto
            {
                TotalRevenue = totalRevenue,
                PendingPayments = pendingPayments,
                CompletedPayments = totalPaidOut,
                PlatformCommissionDeductions = platformCommissionDeductions,
                ModelCommissionDeductions = modelCommissionDeductions,
                TotalDeductions = totalDeductions,
                NetEarnings = netEarnings,
                AvailableForPayout = availableForPayout
            };
        }


        [HttpGet]
        public async Task<ActionResult<IEnumerable<PayoutDto>>> GetPayouts(
            [FromQuery] string? status = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var brandId = GetCurrentBrandId();
            if (string.IsNullOrEmpty(brandId))
            {
                return Unauthorized("Brand not authenticated.");
            }

            var brandUserId = await GetBrandUserIdAsync(brandId);
            if (!brandUserId.HasValue)
            {
                return NotFound("Brand user not found.");
            }

            var query = _context.Payouts
                .Include(p => p.PayeeUser)
                .Where(p => p.PayeeUserId == brandUserId.Value)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                query = query.Where(p => p.Status == status);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(p => p.RequestDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(p => p.RequestDate <= toDate.Value);
            }

            var totalCount = await query.CountAsync();

            var payouts = await query
                .OrderByDescending(p => p.RequestDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PayoutDto
                {
                    Id = p.Id,
                    PayoutId = p.PayoutId,
                    PayeeUserId = p.PayeeUserId,
                    PayeeName = p.PayeeUser.UserName,
                    PayeeEmail = p.PayeeUser.Email,
                    Amount = p.Amount,
                    RequestDate = p.RequestDate,
                    ProcessedDate = p.ProcessedDate,
                    Status = p.Status,
                    Method = p.Method,
                    BankAccountNumber = p.BankAccountNumber,
                    Notes = p.Notes
                })
                .ToListAsync();

            return Ok(new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Payouts = payouts
            });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PayoutDto>> GetPayout(int id)
        {
            var brandId = GetCurrentBrandId();
            if (string.IsNullOrEmpty(brandId))
            {
                return Unauthorized("Brand not authenticated.");
            }

            var brandUserId = await GetBrandUserIdAsync(brandId);
            if (!brandUserId.HasValue)
            {
                return NotFound("Brand user not found.");
            }

            var payout = await _context.Payouts
                .Include(p => p.PayeeUser)
                .FirstOrDefaultAsync(p => p.Id == id && p.PayeeUserId == brandUserId.Value);

            if (payout == null)
            {
                return NotFound();
            }

            var payoutDto = new PayoutDto
            {
                Id = payout.Id,
                PayoutId = payout.PayoutId,
                PayeeUserId = payout.PayeeUserId,
                PayeeName = payout.PayeeUser.UserName,
                PayeeEmail = payout.PayeeUser.Email,
                Amount = payout.Amount,
                RequestDate = payout.RequestDate,
                ProcessedDate = payout.ProcessedDate,
                Status = payout.Status,
                Method = payout.Method,
                BankAccountNumber = payout.BankAccountNumber,
                Notes = payout.Notes
            };

            return payoutDto;
        }

        [HttpPost]
        public async Task<ActionResult<PayoutDto>> CreatePayout(CreatePayoutDto createPayoutDto)
        {
            var brandId = GetCurrentBrandId();
            if (string.IsNullOrEmpty(brandId))
            {
                return Unauthorized("Brand not authenticated.");
            }

            var brandUserId = await GetBrandUserIdAsync(brandId);
            if (!brandUserId.HasValue)
            {
                return NotFound("Brand user not found.");
            }

            var earnings = await GetFinancialEarnings();
            if (createPayoutDto.Amount > earnings.Value.AvailableForPayout)
            {
                return BadRequest($"Insufficient funds. Available for payout: {earnings.Value.AvailableForPayout:C}");
            }

            var lastPayout = await _context.Payouts
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync();

            var nextPayoutNumber = lastPayout != null
                ? int.Parse(lastPayout.PayoutId.Substring(1)) + 1
                : 1;
            var payoutId = $"P{nextPayoutNumber:D3}";

            var payout = new Payout
            {
                PayoutId = payoutId,
                PayeeUserId = brandUserId.Value,
                Amount = createPayoutDto.Amount,
                RequestDate = DateTime.UtcNow,
                Status = "Pending",
                Method = createPayoutDto.Method,
                BankAccountNumber = createPayoutDto.BankAccountNumber,
                Notes = createPayoutDto.Notes
            };

            _context.Payouts.Add(payout);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPayout), new { id = payout.Id }, await GetPayout(payout.Id));
        }

        [HttpGet("available-balance")]
        public async Task<ActionResult<decimal>> GetAvailableBalance()
        {
            var brandId = GetCurrentBrandId();
            if (string.IsNullOrEmpty(brandId))
            {
                return Unauthorized("Brand not authenticated.");
            }

            var earnings = await GetFinancialEarnings();
            return Ok(earnings.Value.AvailableForPayout);
        }

        //[HttpGet("commission-breakdown")]
        //public async Task<ActionResult> GetCommissionBreakdown()
        //{
        //    var brandId = GetCurrentBrandId();
        //    if (string.IsNullOrEmpty(brandId))
        //    {
        //        return Unauthorized("Brand not authenticated.");
        //    }

        //    var platformCommission = await CalculateBrandCommissionDeductions(brandId);
        //    var paidModelCommissions = await _context.ReelCommissions
        //        .Where(rc => rc.BrandId == brandId && rc.Status == "Paid")
        //        .SumAsync(rc => rc.CommissionAmount);

        //    var pendingModelCommissions = await _context.ReelCommissions
        //        .Where(rc => rc.BrandId == brandId && rc.Status == "Pending")
        //        .SumAsync(rc => rc.CommissionAmount);

        //    return Ok(new
        //    {
        //        PlatformCommission = platformCommission,
        //        PaidModelCommissions = paidModelCommissions,
        //        PendingModelCommissions = pendingModelCommissions,
        //        TotalCommissions = platformCommission + paidModelCommissions + pendingModelCommissions
        //    });
        //}

        private bool PayoutExists(int id)
        {
            return _context.Payouts.Any(e => e.Id == id);
        }

        private async Task<decimal> CalculateBrandCommissionDeductions(string brandId)
        {

            var brandOrderItems = await _context.OrderItems
                .Include(oi => oi.Order)
                .Where(oi =>
                    oi.BrandId == brandId &&
                    oi.Order.Status == "Completed" &&
                    oi.Order.PaymentStatus == "Paid")
                .ToListAsync();

            if (!brandOrderItems.Any())
                return 0;


            var brand = await _context.Brands.FindAsync(brandId);
            var globalCommissionRate = await _context.GlobalCommission
                .Select(g => g.CommissionRate)
                .FirstOrDefaultAsync();

            var commissionRate = brand?.SpecificCommissionRate ?? globalCommissionRate;


            decimal totalCommission = brandOrderItems
                .Sum(oi => oi.PriceAtTimeOfPurchase * oi.Quantity * (commissionRate / 100));

            return totalCommission;
        }
    }
}