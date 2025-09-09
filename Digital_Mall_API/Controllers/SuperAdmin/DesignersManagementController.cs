using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.DTOs.SuperAdminDTOs.TShirtDesignersManagementDTOs;
using Digital_Mall_API.Models.Entities.User___Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Controllers.SuperAdmin
{
    [Route("Super/Management/[controller]")]
    [ApiController]
    public class DesignersManagementController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DesignersManagementController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Super/Management/DesignersManagement/Summary
        [HttpGet("Summary")]
        public async Task<IActionResult> GetDesignersSummary()
        {
            var totalDesigners = await _context.TshirtDesigners.CountAsync();
            var activeDesigners = await _context.TshirtDesigners.CountAsync(d => d.Status == "Active");
            var suspendedDesigners = await _context.TshirtDesigners.CountAsync(d => d.Status == "Suspended");

            return Ok(new
            {
                TotalDesigners = totalDesigners,
                ActiveDesigners = activeDesigners,
                SuspendedDesigners = suspendedDesigners
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetDesigners(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _context.TshirtDesigners
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(d =>
                    d.FullName.Contains(search));
            }

            if (!string.IsNullOrEmpty(status) && status != "All Statuses")
            {
                query = query.Where(d => d.Status == status);
            }

            var totalCount = await query.CountAsync();

            query = query.OrderByDescending(d => d.CreatedAt);

            var designers = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new DesignerDto
                {
                    Id = d.Id,
                    UserName = d.FullName,
                    Email = _context.Users.Find(d.Id).Email ?? "Not Found",
                    Status = d.Status,
                    CreatedAt = d.CreatedAt,
                    AssignedRequests = _context.TshirtDesignOrders.Count(dr => dr.Status != "Completed"),
                    TotalEarnings = _context.Payouts
                        .Where(p => p.PayeeUserId.ToString() == d.Id && p.Status == "Completed")
                        .Sum(p => (decimal?)p.Amount) ?? 0m
                })
                .ToListAsync();

            return Ok(new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Designers = designers
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDesignerDetails(string id)
        {
            var designer = await _context.TshirtDesigners
                .Select(d => new DesignerDetailDto
                {
                    Id = d.Id,
                    UserName = d.FullName,
                    Email = _context.Users.Find(d.Id).Email ?? "Not Found",
                    PhoneNumber = _context.Users.Find(d.Id).PhoneNumber ?? "Not Found",
                    ProfilePictureUrl = _context.Users.Find(d.Id).ProfilePictureUrl ?? "Not Found",
                    Status = d.Status,
                    CreatedAt = d.CreatedAt,
                    AssignedRequests = _context.TshirtDesignOrders.Count(dr => dr.Status != "Completed"),
                    CompletedRequests = _context.TshirtDesignOrders.Count(dr => dr.Status == "Completed"),
                    TotalEarnings = _context.Payouts
                        .Where(p => p.PayeeUserId.ToString() == d.Id && p.Status == "Completed")
                        .Sum(p => (decimal?)p.Amount) ?? 0m,
                   
                })
                .FirstOrDefaultAsync(d => d.Id == id);

            if (designer == null)
            {
                return NotFound();
            }

            return Ok(designer);
        }

        [HttpPost]
        public async Task<IActionResult> AddDesigner([FromBody] AddDesignerRequest request)
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return BadRequest("User with this email already exists");
            }

            var user = new ApplicationUser
            {
                UserName = request.UserName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            await _userManager.AddToRoleAsync(user, "Designer");

            var designer = new TshirtDesigner
            {
                Id = user.Id.ToString(),
                FullName = request.UserName,
                Password = request.Password,
                Status = request.Status,
                CreatedAt = DateTime.UtcNow
            };

            _context.TshirtDesigners.Add(designer);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Designer added successfully", DesignerId = user.Id });
        }

        [HttpPut("{id}/Status")]
        public async Task<IActionResult> UpdateDesignerStatus(string id, [FromBody] UpdateStatusRequest request)
        {
            var designer = await _context.TshirtDesigners.FindAsync(id);
            if (designer == null)
            {
                return NotFound();
            }

            designer.Status = request.Status;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Designer status updated successfully" });
        }

        [HttpPut("{id}/Suspend")]
        public async Task<IActionResult> SuspendDesigner(string id)
        {
            var designer = await _context.TshirtDesigners.FindAsync(id);
            if (designer == null)
            {
                return NotFound();
            }

            designer.Status = "Suspended";
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Designer blocked successfully" });
        }

        [HttpPut("{id}/Unsuspend")]
        public async Task<IActionResult> UnsuspendDesigner(string id)
        {
            var designer = await _context.TshirtDesigners.FindAsync(id);
            if (designer == null)
            {
                return NotFound();
            }

            designer.Status = "Active";
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Designer unblocked successfully" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDesigner(string id)
        {
            var designer = await _context.TshirtDesigners
                .FirstOrDefaultAsync(d => d.Id == id);

            if (designer == null)
            {
                return NotFound();
            }

            

            _context.TshirtDesigners.Remove(designer);

            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
            }

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Designer deleted successfully" });
        }

        [HttpGet("StatusOptions")]
        public IActionResult GetStatusOptions()
        {
            var options = new[]
            {
                "All",
                "Active",
                "Suspended"
            };

            return Ok(options);
        }
    }

}