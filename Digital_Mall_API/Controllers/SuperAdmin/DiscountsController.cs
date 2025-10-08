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
        private readonly IWebHostEnvironment _env;
        public DiscountsController(AppDbContext context,IWebHostEnvironment _env)
        {
            _context = context;
            this._env = _env;
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
            [FromQuery] string? status
            ,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _context.Discounts.AsQueryable();

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                query = query.Where(d => d.Status == status);
            }

            

            var totalCount = await query.CountAsync();

            var discounts = await query
                
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new DiscountDto
                {
                    Id = d.Id,
                    ImageUrl = d.ImageUrl,
                    Status = d.Status,
                    
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
                    ImageUrl = d.ImageUrl,
                    Status = d.Status,
                  
                   
                })
                .FirstOrDefaultAsync(d => d.Id == id);

            if (discount == null)
            {
                return NotFound();
            }

            return Ok(discount);
        }
        [HttpPost]
        [Consumes("multipart/form-data")] 
        public async Task<IActionResult> CreateDiscount([FromForm] CreateDiscountRequest request) 
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest("No file uploaded");

            try
            {
                var discount = new Discount
                {
                    Status = request.Status,
                };

                var fileName = $"{Guid.NewGuid()}_{request.File.FileName}";
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "discounts");
                var filePath = Path.Combine(uploadsFolder, fileName);

                Directory.CreateDirectory(uploadsFolder);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await request.File.CopyToAsync(stream);
                }

               
                discount.ImageUrl = $"/uploads/discounts/{fileName}";

                _context.Discounts.Add(discount);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetDiscount), new { id = discount.Id }, discount);
            }
            catch (Exception ex)
            {
                
                return StatusCode(500, "An error occurred while creating the discount");
            }
        }

        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateDiscount(int id, [FromForm] UpdateDiscountRequest request)
        {
            var discount = await _context.Discounts.FindAsync(id);
            if (discount == null)
            {
                return NotFound();
            }

            try
            {
               
                discount.Status = request.Status;

                if (request.File != null && request.File.Length > 0)
                {
                    if (!string.IsNullOrEmpty(discount.ImageUrl))
                    {
                        var oldImagePath = Path.Combine(_env.WebRootPath, discount.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    var fileName = $"{Guid.NewGuid()}_{request.File.FileName}";
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "discounts");
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    Directory.CreateDirectory(uploadsFolder);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await request.File.CopyToAsync(stream);
                    }

                    discount.ImageUrl = $"/uploads/discounts/{fileName}";
                }

                await _context.SaveChangesAsync();

                return Ok(new { Message = "Discount updated successfully" });
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while updating the discount");
            }
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