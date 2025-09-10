using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.DTOs.SuperAdminDTOs.DiscountsDTOs;
using Digital_Mall_API.Models.Entities.Promotions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Controllers.SuperAdmin
{
    [Route("Super/[controller]")]
    [ApiController]
    public class DiscountsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DiscountsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("Summary")]
        public async Task<IActionResult> GetDiscountsSummary()
        {
            var totalDiscounts = await _context.Discounts.CountAsync();
            var activeDiscounts = await _context.Discounts.CountAsync(d => d.Status == "Active");
            var inactiveDiscounts = await _context.Discounts.CountAsync(d => d.Status == "Inactive");

            return Ok(new
            {
                TotalDiscounts = totalDiscounts,
                ActiveDiscounts = activeDiscounts,
                InactiveDiscounts = inactiveDiscounts
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetDiscounts(
            [FromQuery] string? status,
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _context.Discounts.AsQueryable();

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                query = query.Where(d => d.Status == status);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(d =>
                    
                    d.Description.Contains(search));
            }

            var totalCount = await query.CountAsync();

            var discounts = await query
                .OrderByDescending(d => d.StartDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new DiscountDto
                {
                    Id = d.Id,
                    Description = d.Description,
                    Status = d.Status,
                    StartDate = d.StartDate,
                    EndDate = d.EndDate,
                })
                .ToListAsync();

            return Ok(new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Discounts = discounts
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDiscount(int id)
        {
            var discount = await _context.Discounts
                .Select(d => new DiscountDetailDto
                {
                    Id = d.Id,
                    Description = d.Description,
                    Status = d.Status,
                    StartDate = d.StartDate,
                    EndDate = d.EndDate,
                   
                })
                .FirstOrDefaultAsync(d => d.Id == id);

            if (discount == null)
            {
                return NotFound();
            }

            return Ok(discount);
        }

        [HttpPost]
        public async Task<IActionResult> CreateDiscount([FromBody] CreateDiscountRequest request)
        {
            var discount = new Discount
            {
                Description = request.Description,
                Status = request.Status,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
            };

            _context.Discounts.Add(discount);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDiscount), new { id = discount.Id }, discount);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDiscount(int id, [FromBody] UpdateDiscountRequest request)
        {
            var discount = await _context.Discounts.FindAsync(id);
            if (discount == null)
            {
                return NotFound();
            }

            discount.Description = request.Description;
            discount.Status = request.Status;
            discount.StartDate = request.StartDate;
            discount.EndDate = request.EndDate;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Discount updated successfully" });
        }

        

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDiscount(int id)
        {
            var discount = await _context.Discounts.FindAsync(id);
            if (discount == null)
            {
                return NotFound();
            }

            _context.Discounts.Remove(discount);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Discount deleted successfully" });
        }

        [HttpGet("StatusOptions")]
        public IActionResult GetStatusOptions()
        {
            var options = new[]
            {
                "All",
                "Active",
                "Inactive"
            };

            return Ok(options);
        }
    }

    
}