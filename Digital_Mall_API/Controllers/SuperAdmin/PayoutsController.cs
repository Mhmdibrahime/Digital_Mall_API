using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.DTOs.SuperAdminDTOs.PayoutsDTOs;
using Digital_Mall_API.Models.Entities.Financials;
using Digital_Mall_API.Models.Entities.User___Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Controllers.SuperAdmin
{
    [Route("Super/Financial/[controller]")]
    [ApiController]
    public class PayoutsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;

        public PayoutsController(AppDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole<Guid>> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        private async Task<string> GetUserTypeAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Brand")) return "Brand";
            if (roles.Contains("Model")) return "Model";
            if (roles.Contains("Designer")) return "Designer";

            return roles.FirstOrDefault() ?? "Unknown";
        }

        [HttpGet("PayoutsSummary")]
        public async Task<IActionResult> GetPayoutsSummary()
        {
            var totalPayouts = await _context.Payouts.CountAsync();
            var pendingPayouts = await _context.Payouts.CountAsync(p => p.Status == "Pending");
            var approvedPayouts = await _context.Payouts.CountAsync(p => p.Status == "Approved");
            var paidPayouts = await _context.Payouts.CountAsync(p => p.Status == "Paid");
            var rejectedPayouts = await _context.Payouts.CountAsync(p => p.Status == "Rejected");

            return Ok(new
            {
                TotalPayouts = totalPayouts,
                PendingPayouts = pendingPayouts,
                ApprovedPayouts = approvedPayouts,
                PaidPayouts = paidPayouts,
                RejectedPayouts = rejectedPayouts
            });
        }

       
        [HttpGet("Pending")]
        public async Task<IActionResult> GetPendingPayouts()
        {
            var pendingPayouts = await _context.Payouts
                .Include(p => p.PayeeUser)
                .Where(p => p.Status == "Pending")
                .OrderByDescending(p => p.RequestDate)
                .ToListAsync();

            var result = new List<PayoutDto>();
            foreach (var payout in pendingPayouts)
            {
                var userType = await GetUserTypeAsync(payout.PayeeUser);
                result.Add(new PayoutDto
                {
                    Id = payout.Id,
                    PayeeName = payout.PayeeUser.UserName,
                    PayeeEmail = payout.PayeeUser.Email,
                    PayeeType = userType,
                    Amount = payout.Amount,
                    Status = payout.Status,
                    RequestDate = payout.RequestDate,
                    BankAccount = payout.BankAccountNumber
                });
            }

            return Ok(result);
        }

        [HttpGet("Approved")]
        public async Task<IActionResult> GetApprovedPayouts()
        {
            var approvedPayouts = await _context.Payouts
                .Include(p => p.PayeeUser)
                .Where(p => p.Status == "Approved")
                .OrderByDescending(p => p.RequestDate)
                .ToListAsync();

            var result = new List<PayoutDto>();
            foreach (var payout in approvedPayouts)
            {
                var userType = await GetUserTypeAsync(payout.PayeeUser);
                result.Add(new PayoutDto
                {
                    Id = payout.Id,
                    PayeeName = payout.PayeeUser.UserName,
                    PayeeEmail = payout.PayeeUser.Email,
                    PayeeType = userType,
                    Amount = payout.Amount,
                    Status = payout.Status,
                    RequestDate = payout.RequestDate,
                    BankAccount = payout.BankAccountNumber
                });
            }

            return Ok(result);
        }

        [HttpGet("Paid")]
        public async Task<IActionResult> GetPaidPayouts()
        {
            var paidPayouts = await _context.Payouts
                .Include(p => p.PayeeUser)
                .Where(p => p.Status == "Paid")
                .OrderByDescending(p => p.ProcessedDate)
                .ToListAsync();

            var result = new List<PayoutDto>();
            foreach (var payout in paidPayouts)
            {
                var userType = await GetUserTypeAsync(payout.PayeeUser);
                result.Add(new PayoutDto
                {
                    Id = payout.Id,
                    PayeeName = payout.PayeeUser.UserName,
                    PayeeEmail = payout.PayeeUser.Email,
                    PayeeType = userType,
                    Amount = payout.Amount,
                    Status = payout.Status,
                    RequestDate = payout.RequestDate,
                    ProcessedDate = payout.ProcessedDate,
                    BankAccount = payout.BankAccountNumber
                });
            }

            return Ok(result);
        }

        [HttpGet("Rejected")]
        public async Task<IActionResult> GetRejectedPayouts()
        {
            var rejectedPayouts = await _context.Payouts
                .Include(p => p.PayeeUser)
                .Where(p => p.Status == "Rejected")
                .OrderByDescending(p => p.ProcessedDate)
                .ToListAsync();

            var result = new List<PayoutDto>();
            foreach (var payout in rejectedPayouts)
            {
                var userType = await GetUserTypeAsync(payout.PayeeUser);
                result.Add(new PayoutDto
                {
                    Id = payout.Id,
                    PayeeName = payout.PayeeUser.UserName,
                    PayeeEmail = payout.PayeeUser.Email,
                    PayeeType = userType,
                    Amount = payout.Amount,
                    Status = payout.Status,
                    RequestDate = payout.RequestDate,
                    ProcessedDate = payout.ProcessedDate,
                    BankAccount = payout.BankAccountNumber
                });
            }

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPayoutDetails(int id)
        {
            var payout = await _context.Payouts
                .Include(p => p.PayeeUser)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payout == null)
            {
                return NotFound();
            }

            var userType = await GetUserTypeAsync(payout.PayeeUser);

            var payoutDetails = new PayoutDetailDto
            {
                Id = payout.Id,
                PayeeName = payout.PayeeUser.UserName,
                PayeeEmail = payout.PayeeUser.Email,
                PayeeType = userType,
                Amount = payout.Amount,
                Status = payout.Status,
                RequestDate = payout.RequestDate,
                ProcessedDate = payout.ProcessedDate,
                BankAccount = payout.BankAccountNumber
            };

            return Ok(payoutDetails);
        }

        [HttpPut("{id}/Approve")]
        public async Task<IActionResult> ApprovePayout(int id)
        {
            var payout = await _context.Payouts.FindAsync(id);
            if (payout == null)
            {
                return NotFound();
            }

            payout.Status = "Approved";
            payout.ProcessedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Payout approved successfully" });
        }

        [HttpPut("{id}/Reject")]
        public async Task<IActionResult> RejectPayout(int id)
        {
            var payout = await _context.Payouts.FindAsync(id);
            if (payout == null)
            {
                return NotFound();
            }

            payout.Status = "Rejected";
            payout.ProcessedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Payout rejected successfully" });
        }

        [HttpPut("{id}/MarkAsPaid")]
        public async Task<IActionResult> MarkAsPaid(int id)
        {
            var payout = await _context.Payouts.FindAsync(id);
            if (payout == null)
            {
                return NotFound();
            }

            if (payout.Status != "Approved")
            {
                return BadRequest("Only approved payouts can be marked as paid");
            }

            payout.Status = "Paid";
            payout.ProcessedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Payout marked as paid successfully" });
        }


       
    }

    
}