using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.DTOs.SuperAdminDTOs.TShirtDesignersManagementDTOs;
using Digital_Mall_API.Models.Entities.User___Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Digital_Mall_API.Controllers.SuperAdmin
{
    [Route("Super/Management/[controller]")]
    [ApiController]
    public class DesignersManagementController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;


        public DesignersManagementController(AppDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole<Guid>> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;

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

        [HttpGet("GetDesigners")]
        public async Task<IActionResult> GetDesigners(string? search, string? status, int page = 1, int pageSize = 20)
        {
            var query =
                from d in _context.TshirtDesigners
                join u in _context.Users on d.Id equals u.Id.ToString() into userGroup
                from u in userGroup.DefaultIfEmpty() // Left join — allows designers without a user
                select new
                {
                    Designer = d,
                    User = u
                };

            if (!string.IsNullOrEmpty(search))
                query = query.Where(x => x.Designer.FullName.Contains(search));

            if (!string.IsNullOrEmpty(status) && status != "All Statuses")
                query = query.Where(x => x.Designer.Status == status);

            // Count total results (before pagination)
            var totalCount = await query.CountAsync();

            // Fetch paginated data
            var designers = await query
                .OrderByDescending(x => x.Designer.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new DesignerDto
                {
                    Id = x.Designer.Id,
                    UserName = x.Designer.FullName,
                    Email = x.User != null ? x.User.Email : "Not Found",
                    Status = x.Designer.Status,
                    CreatedAt = x.Designer.CreatedAt,
                    AssignedRequests = _context.TshirtDesignOrders
                        .Count(dr => dr.Status != "Completed"),
                    TotalEarnings = _context.Payouts
                        .Where(p => p.PayeeUserId.ToString() == x.Designer.Id && p.Status == "Completed")
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
        public async Task<IActionResult> GetDesignerDetails(Guid id)
        {
            var designerEntity = await _context.TshirtDesigners
                .FirstOrDefaultAsync(d => d.Id == id.ToString());

            if (designerEntity == null)
                return NotFound();

            var user = await _context.Users.FindAsync(id);

            var designer = new DesignerDetailDto
            {
                Id = designerEntity.Id,
                UserName = designerEntity.FullName,
                Email = user?.Email ?? "Not Found",
                PhoneNumber = user?.PhoneNumber ?? "Not Found",
                ProfilePictureUrl = user?.ProfilePictureUrl ?? "Not Found",
                Status = designerEntity.Status,
                CreatedAt = designerEntity.CreatedAt,
                AssignedRequests = await _context.TshirtDesignOrders.CountAsync(dr => dr.Status != "Completed"),
                CompletedRequests = await _context.TshirtDesignOrders.CountAsync(dr => dr.Status == "Completed"),
                TotalEarnings = await _context.Payouts
                    .Where(p => p.PayeeUserId.ToString() == designerEntity.Id && p.Status == "Completed")
                    .SumAsync(p => (decimal?)p.Amount) ?? 0m
            };

            return Ok(designer);
        }


        [HttpPost("AddDesigner")]
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
            if (!await _roleManager.RoleExistsAsync("Designer"))
                await _roleManager.CreateAsync(new IdentityRole<Guid>("Designer"));

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
        public async Task<IActionResult> UpdateDesignerStatus(Guid id, [FromBody] UpdateStatusRequest request)
        {
            var designer = await _context.TshirtDesigners.FindAsync(id.ToString());
            if (designer == null)
            {
                return NotFound();
            }

            designer.Status = request.Status;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Designer status updated successfully" });
        }

        [HttpPut("{id}/Suspend")]
        public async Task<IActionResult> SuspendDesigner(Guid id)
        {
            var designer = await _context.TshirtDesigners.FindAsync(id.ToString());
            if (designer == null)
            {
                return NotFound();
            }

            designer.Status = "Suspended";
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Designer blocked successfully" });
        }

        [HttpPut("{id}/Unsuspend")]
        public async Task<IActionResult> UnsuspendDesigner(Guid id)
        {
            var designer = await _context.TshirtDesigners.FindAsync(id.ToString());
            if (designer == null)
            {
                return NotFound();
            }

            designer.Status = "Active";
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Designer unblocked successfully" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDesigner(Guid id)
        {
            var designer = await _context.TshirtDesigners
                .FirstOrDefaultAsync(d => d.Id == id.ToString());

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