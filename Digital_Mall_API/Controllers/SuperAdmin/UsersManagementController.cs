using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.DTOs.SuperAdminDTOs.UsersManagementDTOs;
using Digital_Mall_API.Models.Entities.Orders___Shopping;
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
    public class UsersManagementController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UsersManagementController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("Summary")]
        public async Task<IActionResult> GetUsersSummary()
        {
            var totalUsers = await _context.Customers.CountAsync();
            var activeUsers = await _context.Customers.CountAsync(c => c.Status == "Active");
            var blockedUsers = await _context.Customers.CountAsync(c => c.Status == "Blocked");

            return Ok(new
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                BlockedUsers = blockedUsers
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 30)
        {
            var query = _context.Customers
                .Include(c => c.Orders)
                .Include(c => c.DesignOrders)
                .AsQueryable();

            var userQuery = from customer in query
                            join user in _context.Users on customer.Id equals user.Id
                            select new { Customer = customer, User = user };

            if (!string.IsNullOrEmpty(search))
            {
                userQuery = userQuery.Where(x =>
                    x.User.UserName.Contains(search) ||
                    x.User.Email.Contains(search) ||
                    x.User.DisplayName.Contains(search));
            }

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                userQuery = userQuery.Where(x => x.Customer.Status == status);
            }

            var totalCount = await userQuery.CountAsync();

            userQuery = userQuery.OrderByDescending(x => x.Customer.CreatedAt);

            var users = await userQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new UserDto
                {
                    Id = x.User.Id,
                    UserName = x.User.UserName,
                    DisplayName = x.User.DisplayName,
                    Email = x.User.Email,
                    Status = x.Customer.Status,
                    CreatedAt = x.Customer.CreatedAt,
                    OrdersCount = x.Customer.Orders.Count(o => o.Status == "completed" || o.Status == "delivered"),
                    TotalSpent = x.Customer.Orders
                        .Where(o => o.Status == "completed" || o.Status == "delivered")
                        .Sum(o => (decimal?)o.TotalAmount) ?? 0m,
                    DesignOrdersCount = x.Customer.DesignOrders.Count
                })
                .ToListAsync();

            return Ok(new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Users = users
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserDetails(Guid id)
        {
            var customerId = id.ToString();

            var result = await (from customer in _context.Customers
                                where customer.Id.ToString() == customerId
                                join user in _context.Users on customer.Id equals user.Id
                                select new UserDetailDto
                                {
                                    Id = user.Id,
                                    UserName = user.UserName,
                                    DisplayName = user.DisplayName,
                                    Email = user.Email,
                                    PhoneNumber = user.PhoneNumber,
                                    ProfilePictureUrl = user.ProfilePictureUrl,
                                    Status = customer.Status,
                                    CreatedAt = customer.CreatedAt,
                                    OrdersCount = customer.Orders.Count(o => o.Status == "completed" || o.Status == "delivered"),
                                    TotalSpent = customer.Orders
                                        .Where(o => o.Status == "completed" || o.Status == "delivered")
                                        .Sum(o => (decimal?)o.TotalAmount) ?? 0m,
                                    DesignOrdersCount = customer.DesignOrders.Count,
                                    LastOrderDate = customer.Orders
                                        .Where(o => o.Status == "completed" || o.Status == "delivered")
                                        .OrderByDescending(o => o.OrderDate)
                                        .Select(o => (DateTime?)o.OrderDate)
                                        .FirstOrDefault(),
                                    AverageOrderValue = customer.Orders.Count > 0 ?
                                        customer.Orders
                                            .Where(o => o.Status == "completed" || o.Status == "delivered")
                                            .Average(o => (decimal?)o.TotalAmount) ?? 0m : 0m
                                })
                                .FirstOrDefaultAsync();

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        [HttpPut("{id}/Block")]
        public async Task<IActionResult> BlockUser(Guid id)
        {
            var customerId = id.ToString();
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null)
            {
                return NotFound();
            }

            customer.Status = "Blocked";

            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user != null)
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.Now.AddYears(100));
            }

            await _context.SaveChangesAsync();

            return Ok(new { Message = "User blocked successfully" });
        }

        [HttpPut("{id}/Unblock")]
        public async Task<IActionResult> UnblockUser(Guid id)
        {
            var customerId = id.ToString();
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null)
            {
                return NotFound();
            }

            customer.Status = "Active";

            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user != null)
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                await _userManager.ResetAccessFailedCountAsync(user);
            }

            await _context.SaveChangesAsync();

            return Ok(new { Message = "User unblocked successfully" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var customerId = id.ToString();
            var customer = await _context.Customers
                .Include(c => c.Orders)
                .Include(c => c.DesignOrders)
                .FirstOrDefaultAsync(c => c.Id.ToString() == customerId);

            if (customer == null)
            {
                return NotFound();
            }

            if (customer.Orders.Any() || customer.DesignOrders.Any())
            {
                return BadRequest("Cannot delete user with associated orders");
            }

            _context.Customers.Remove(customer);

            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }

            await _context.SaveChangesAsync();

            return Ok(new { Message = "User deleted successfully" });
        }

        [HttpGet("StatusOptions")]
        public IActionResult GetStatusOptions()
        {
            var options = new[]
            {
                "All",
                "Active",
                "Blocked"
            };

            return Ok(options);
        }
    }

    
   
}