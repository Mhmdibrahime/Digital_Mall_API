using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.DTOs.BrandAdminDTOs.BrandPayoutsDTOs;
using Digital_Mall_API.Models.Entities.Financials;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Digital_Mall_API.Controllers.Model
{
    [Route("Model/[controller]")]
    [ApiController]
    public class ModelPayoutsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ModelPayoutsController(AppDbContext context)
        {
            _context = context;
        }

        private string GetCurrentModelId()
        {
            var modelId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return modelId;
        }

        private async Task<Guid?> GetModelUserIdAsync(string modelId)
        {
            var modelUser = await _context.Users.FirstOrDefaultAsync(u => u.Id.ToString() == modelId);
            return modelUser?.Id;
        }

        [HttpGet("Earnings")]
        private async Task<ActionResult<ModelEarningsDto>> GetModelEarnings()
        {
            var modelId = GetCurrentModelId();
            if (string.IsNullOrEmpty(modelId))
            {
                return Unauthorized("Model not authenticated.");
            }

            var totalCommissions = await _context.ReelCommissions
                .Where(rc => rc.FashionModelId == modelId)
                .SumAsync(rc => rc.CommissionAmount);

            var paidCommissions = await _context.ReelCommissions
                .Where(rc => rc.FashionModelId == modelId && rc.Status == "Paid")
                .SumAsync(rc => rc.CommissionAmount);

            var pendingCommissions = await _context.ReelCommissions
                .Where(rc => rc.FashionModelId == modelId && rc.Status == "Pending")
                .SumAsync(rc => rc.CommissionAmount);

            var modelUserId = await GetModelUserIdAsync(modelId);
            var totalPaidOut = modelUserId.HasValue
                ? await _context.Payouts
                    .Where(p => p.PayeeUserId == modelUserId.Value && p.Status == "Completed")
                    .SumAsync(p => p.Amount)
                : 0;

            var pendingPayments = modelUserId.HasValue
                ? await _context.Payouts
                    .Where(p => p.PayeeUserId == modelUserId.Value && (p.Status == "Approved" || p.Status == "Pending"))
                    .SumAsync(p => p.Amount)
                : 0;

            var availableForPayout = Math.Max(0, paidCommissions - totalPaidOut);

            return new ModelEarningsDto
            {
                TotalCommissions = totalCommissions,
                PaidCommissions = paidCommissions,
                PendingCommissions = pendingCommissions,
                TotalPaidOut = totalPaidOut,
                PendingPayments = pendingPayments,
                AvailableForPayout = availableForPayout
            };
        }

        [HttpGet("GetPayouts")]
        public async Task<ActionResult<IEnumerable<PayoutDto>>> GetPayouts(
           
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var modelId = GetCurrentModelId();
            if (string.IsNullOrEmpty(modelId))
            {
                return Unauthorized("Model not authenticated.");
            }

            var modelUserId = await GetModelUserIdAsync(modelId);
            if (!modelUserId.HasValue)
            {
                return NotFound("Model user not found.");
            }

            var query = _context.Payouts
                .Include(p => p.PayeeUser)
                .Where(p => p.PayeeUserId == modelUserId.Value)
                .AsQueryable();

          

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

        [HttpGet("GetPayout/{id}")]
        public async Task<ActionResult<PayoutDto>> GetPayout(int id)
        {
            var modelId = GetCurrentModelId();
            if (string.IsNullOrEmpty(modelId))
            {
                return Unauthorized("Model not authenticated.");
            }

            var modelUserId = await GetModelUserIdAsync(modelId);
            if (!modelUserId.HasValue)
            {
                return NotFound("Model user not found.");
            }

            var payout = await _context.Payouts
                .Include(p => p.PayeeUser)
                .FirstOrDefaultAsync(p => p.Id == id && p.PayeeUserId == modelUserId.Value);

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

        [HttpPost("CreatePayout")]
        public async Task<ActionResult<PayoutDto>> CreatePayout(CreatePayoutDto createPayoutDto)
        {
            var modelId = GetCurrentModelId();
            if (string.IsNullOrEmpty(modelId))
            {
                return Unauthorized("Model not authenticated.");
            }

            var modelUserId = await GetModelUserIdAsync(modelId);
            if (!modelUserId.HasValue)
            {
                return NotFound("Model user not found.");
            }

            var earnings = await GetModelEarnings();
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
            var payoutId = $"M{nextPayoutNumber:D3}";

            var payout = new Payout
            {
                PayoutId = payoutId,
                PayeeUserId = modelUserId.Value,
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
            var modelId = GetCurrentModelId();
            if (string.IsNullOrEmpty(modelId))
            {
                return Unauthorized("Model not authenticated.");
            }

            var earnings = await GetModelEarnings();
            return Ok(earnings.Value.AvailableForPayout);
        }

       
        private bool PayoutExists(int id)
        {
            return _context.Payouts.Any(e => e.Id == id);
        }
    }

    public class ModelEarningsDto
    {
        public decimal TotalCommissions { get; set; }
        public decimal PaidCommissions { get; set; }
        public decimal PendingCommissions { get; set; }
        public decimal TotalPaidOut { get; set; }
        public decimal PendingPayments { get; set; }
        public decimal AvailableForPayout { get; set; }
    }
}