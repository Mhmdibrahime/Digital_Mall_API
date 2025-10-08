using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.DTOs.SuperAdminDTOs.ModelsManagementDTOs;
using Digital_Mall_API.Models.Entities.Financials;
using Digital_Mall_API.Models.Entities.Reels___Content;
using Digital_Mall_API.Models.Entities.User___Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Controllers.SuperAdmin
{
    [Route("Super/Management/[controller]")]
    [ApiController]
    public class ModelsManagementController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ModelsManagementController(AppDbContext context)
        {
            _context = context;
        }
        private void GlobalCommissionRate()
        {
            var globalCommission = _context.GlobalCommission.FirstOrDefault();
            if (globalCommission == null)
            {
                globalCommission = new GlobalCommission
                {
                    CommissionRate = 10m
                };

                _context.GlobalCommission.Add(globalCommission);
                _context.SaveChanges();
            }
        }
        [HttpGet("Summary")]
        public async Task<IActionResult> GetModelsSummary()
        {
            var totalModels = await _context.FashionModels.CountAsync();
            var activeModels = await _context.FashionModels.CountAsync(m => m.Status == "Active");
            var pendingModels = await _context.FashionModels.CountAsync(m => m.Status == "Pending");
            var suspendedModels = await _context.FashionModels.CountAsync(m => m.Status == "Suspended");

            return Ok(new
            {
                TotalModels = totalModels,
                ActiveModels = activeModels,
                PendingModels = pendingModels,
                SuspendedModels = suspendedModels
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetModels(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            GlobalCommissionRate();
            var query = _context.FashionModels
                
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(m =>
                    m.Name.Contains(search) ||
                    m.Bio.Contains(search));
            }

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                query = query.Where(m => m.Status == status);
            }

            var totalCount = await query.CountAsync();

            query = query.OrderByDescending(m => m.CreatedAt);

            var models = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new ModelDto
                {
                    Id = m.Id,
                    FullName = m.Name,
                    Bio = m.Bio,
                    Status = m.Status,
                    CommissionRate = m.SpecificCommissionRate ?? _context.GlobalCommission.FirstOrDefault().CommissionRate,
                    CreatedAt = m.CreatedAt,
                    ReelsCount = m.Reels.Count,
                    TotalLikes = m.Reels.Sum(r => r.LikesCount),
                    TotalEarnings = m.Payouts
                        .Where(p => p.Status == "Completed")
                        .Sum(p => (decimal?)p.Amount) ?? 0m,

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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetModelDetails(string id)
        {
            var user = await _context.Users.FindAsync(id);
            var model = await _context.FashionModels
                .Include(m => m.Reels)
                .Include(m => m.Payouts)
                .Select(m => new ModelDetailDto
                {
                    Id = m.Id,
                    FullName = m.Name,
                    Email = user.Email ?? "Not Found",
                    Phone = user.PhoneNumber ?? "Not Found",
                    
                    Bio = m.Bio,
                    Status = m.Status,
                    CommissionRate = m.SpecificCommissionRate ?? _context.GlobalCommission.FirstOrDefault().CommissionRate,
                    EvidenceOfProofUrl = m.EvidenceOfProofUrl,
                    CreatedAt = m.CreatedAt,
                    ReelsCount = m.Reels.Count,
                    TotalLikes = m.Reels.Sum(r => r.LikesCount),
                    AverageLikesPerReel = m.Reels.Count > 0 ? m.Reels.Average(r => r.LikesCount) : 0,
                    TotalEarnings = m.Payouts
                        .Where(p => p.Status == "Completed")
                        .Sum(p => (decimal?)p.Amount) ?? 0m,
                    PendingEarnings = m.Payouts
                        .Where(p => p.Status == "Pending")
                        .Sum(p => (decimal?)p.Amount) ?? 0m,
                  


                })
                .FirstOrDefaultAsync(m => m.Id == id);

            if (model == null)
            {
                return NotFound();
            }

            return Ok(model);
        }

        [HttpGet("{id}/ModelReels")]
        public async Task<IActionResult> GetModelReels(string id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var reels = await _context.Reels
                .Where(r => r.PostedByUserId == id)
                .OrderByDescending(r => r.PostedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new ReelDetailDto
                {
                    Id = r.Id,
                    VideoUrl = r.VideoUrl,
                    ThumbnailUrl = r.ThumbnailUrl,
                    Caption = r.Caption,
                    PostedDate = r.PostedDate,
                    DurationInSeconds = r.DurationInSeconds,
                    LikesCount = r.LikesCount,
                    LinkedProductsCount = r.LinkedProducts.Count
                })
                .ToListAsync();

            var totalCount = await _context.Reels.CountAsync(r => r.PostedByUserId == id);

            return Ok(new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Reels = reels
            });
        }

        [HttpPut("{id}/UpdateStatus")]
        public async Task<IActionResult> UpdateModelStatus(Guid id, [FromBody] string status)
        {
            var model = await _context.FashionModels.FindAsync(id);
            if (model == null)
            {
                return NotFound();
            }

            model.Status = status;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Model status updated successfully" });
        }

        [HttpPut("{id}/EditCommission")]
        public async Task<IActionResult> UpdateCommissionRate(Guid id, [FromBody] decimal commissionRate)
        {
            if (commissionRate < 0 || commissionRate > 100)
            {
                return BadRequest("Commission rate must be between 0 and 100");
            }

            var model = await _context.FashionModels.FindAsync(id);
            if (model == null)
            {
                return NotFound();
            }

            model.SpecificCommissionRate = commissionRate;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Commission rate updated successfully" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteModel(string id)
        {
            var model = await _context.FashionModels
                .FirstOrDefaultAsync(m => m.Id == id);

            if (model == null)
            {
                return NotFound();
            }

            var hasReels = await _context.Reels.AnyAsync(r => r.PostedByUserId == id);
            var hasPayouts = await _context.Payouts.AnyAsync(p => p.PayeeUserId.ToString() == id && p.Status == "Pending");

            if (hasReels || hasPayouts)
            {
                return BadRequest("Cannot delete model with associated reels or payouts");
            }

            _context.FashionModels.Remove(model);

            var user = await _context.Users.FindAsync(id);

            if (user != null)
            {
                _context.Users.Remove(user);
            }

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Model deleted successfully" });
        }

        [HttpGet("StatusOptions")]
        public IActionResult GetStatusOptions()
        {
            var options = new[]
            {
                "All",
                "Active",
                "Pending",
                "Suspended",
                "Rejected"
            };

            return Ok(options);
        }



        private async Task<decimal> CalculatePerformanceScore(string modelId)
        {
            var totalLikes = await _context.Reels
                .Where(r => r.PostedByUserId == modelId)
                .SumAsync(r => (int?)r.LikesCount) ?? 0;

            var reelsCount = await _context.Reels
                .CountAsync(r => r.PostedByUserId == modelId);

            var earnings = await _context.Payouts
                .Where(p => p.PayeeUserId.ToString() == modelId && p.Status == "Completed")
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            return totalLikes + (reelsCount * 100) + (earnings * 10);
        }
    }

    
}