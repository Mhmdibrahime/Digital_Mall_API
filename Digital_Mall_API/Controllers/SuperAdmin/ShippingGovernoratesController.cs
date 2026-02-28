using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.Entities.Orders___Shopping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Controllers.SuperAdmin
{
    [Route("Super/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class ShippingGovernoratesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ShippingGovernoratesController(AppDbContext context)
        {
            _context = context;
        }

        // DTO Classes
        public class ShippingGovernorateDto
        {
            public int Id { get; set; }
            public string EnglishName { get; set; }
            public string ArabicName { get; set; }
            public decimal Price { get; set; }
        }

        public class CreateShippingGovernorateDto
        {
            [Required]
            public string EnglishName { get; set; }

            [Required]
            public string ArabicName { get; set; }

            [Required]
            [Range(0, double.MaxValue)]
            public decimal Price { get; set; }
        }

        public class UpdateShippingGovernorateDto
        {
            [Required]
            public string EnglishName { get; set; }

            [Required]
            public string ArabicName { get; set; }

            [Required]
            [Range(0, double.MaxValue)]
            public decimal Price { get; set; }
        }

        // Seed Egyptian Governorates
        private async Task SeedEgyptianGovernorates()
        {
            if (!await _context.ShippingGovernorates.AnyAsync())
            {
                var governorates = new List<ShippingGovernorate>
                {
                    new() { EnglishName = "Cairo", ArabicName = "القاهرة", Price = 50 },
                    new() { EnglishName = "Alexandria", ArabicName = "الإسكندرية", Price = 60 },
                    new() { EnglishName = "Giza", ArabicName = "الجيزة", Price = 45 },
                    new() { EnglishName = "Sharkia", ArabicName = "الشرقية", Price = 70 },
                    new() { EnglishName = "Dakahlia", ArabicName = "الدقهلية", Price = 70 },
                    new() { EnglishName = "Beheira", ArabicName = "البحيرة", Price = 75 },
                    new() { EnglishName = "Monufia", ArabicName = "المنوفية", Price = 65 },
                    new() { EnglishName = "Qalyubia", ArabicName = "القليوبية", Price = 55 },
                    new() { EnglishName = "Gharbia", ArabicName = "الغربية", Price = 65 },
                    new() { EnglishName = "Port Said", ArabicName = "بورسعيد", Price = 80 },
                    new() { EnglishName = "Suez", ArabicName = "السويس", Price = 80 },
                    new() { EnglishName = "Ismailia", ArabicName = "الإسماعيلية", Price = 75 },
                    new() { EnglishName = "Faiyum", ArabicName = "الفيوم", Price = 85 },
                    new() { EnglishName = "Beni Suef", ArabicName = "بني سويف", Price = 90 },
                    new() { EnglishName = "Minya", ArabicName = "المنيا", Price = 95 },
                    new() { EnglishName = "Asyut", ArabicName = "أسيوط", Price = 100 },
                    new() { EnglishName = "Sohag", ArabicName = "سوهاج", Price = 110 },
                    new() { EnglishName = "Qena", ArabicName = "قنا", Price = 115 },
                    new() { EnglishName = "Luxor", ArabicName = "الأقصر", Price = 120 },
                    new() { EnglishName = "Aswan", ArabicName = "أسوان", Price = 130 },
                    new() { EnglishName = "Red Sea", ArabicName = "البحر الأحمر", Price = 150 },
                    new() { EnglishName = "New Valley", ArabicName = "الوادي الجديد", Price = 180 },
                    new() { EnglishName = "Matrouh", ArabicName = "مطروح", Price = 160 },
                    new() { EnglishName = "North Sinai", ArabicName = "شمال سيناء", Price = 170 },
                    new() { EnglishName = "South Sinai", ArabicName = "جنوب سيناء", Price = 170 },
                    new() { EnglishName = "Damietta", ArabicName = "دمياط", Price = 70 },
                    new() { EnglishName = "Kafr El Sheikh", ArabicName = "كفر الشيخ", Price = 75 }
                };

                await _context.ShippingGovernorates.AddRangeAsync(governorates);
                await _context.SaveChangesAsync();
            }
        }

        // GET: api/ShippingGovernorates
        [HttpGet]
        public async Task<IActionResult> GetShippingGovernorates(
            [FromQuery] string? search,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            // Check if table is empty and seed Egyptian governorates
            await SeedEgyptianGovernorates();

            var query = _context.ShippingGovernorates.AsQueryable();

            // Search functionality
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(g =>
                    g.EnglishName.Contains(search) ||
                    g.ArabicName.Contains(search));
            }

            // Price filtering
            if (minPrice.HasValue)
            {
                query = query.Where(g => g.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(g => g.Price <= maxPrice.Value);
            }

            var totalCount = await query.CountAsync();

            // Ordering
            query = query.OrderBy(g => g.EnglishName);

            // Pagination
            var governorates = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(g => new ShippingGovernorateDto
                {
                    Id = g.Id,
                    EnglishName = g.EnglishName,
                    ArabicName = g.ArabicName,
                    Price = g.Price
                })
                .ToListAsync();

            return Ok(new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Governorates = governorates
            });
        }

        // GET: api/ShippingGovernorates/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetShippingGovernorate(int id)
        {
            var governorate = await _context.ShippingGovernorates
                .Where(g => g.Id == id)
                .Select(g => new ShippingGovernorateDto
                {
                    Id = g.Id,
                    EnglishName = g.EnglishName,
                    ArabicName = g.ArabicName,
                    Price = g.Price
                })
                .FirstOrDefaultAsync();

            if (governorate == null)
            {
                return NotFound("Shipping governorate not found");
            }

            return Ok(governorate);
        }

        // POST: api/ShippingGovernorates
        [HttpPost]
        public async Task<IActionResult> CreateShippingGovernorate([FromForm] CreateShippingGovernorateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if governorate with same name already exists
            var existingGovernorate = await _context.ShippingGovernorates
                .FirstOrDefaultAsync(g =>
                    g.EnglishName.ToLower() == dto.EnglishName.ToLower() ||
                    g.ArabicName == dto.ArabicName);

            if (existingGovernorate != null)
            {
                return Conflict("Governorate with this name already exists");
            }

            var governorate = new ShippingGovernorate
            {
                EnglishName = dto.EnglishName,
                ArabicName = dto.ArabicName,
                Price = dto.Price,
                CreatedAt = DateTime.UtcNow
            };

            _context.ShippingGovernorates.Add(governorate);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetShippingGovernorate), new { id = governorate.Id }, new ShippingGovernorateDto
            {
                Id = governorate.Id,
                EnglishName = governorate.EnglishName,
                ArabicName = governorate.ArabicName,
                Price = governorate.Price
            });
        }

        // PUT: api/ShippingGovernorates/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateShippingGovernorate(int id, [FromForm] UpdateShippingGovernorateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var governorate = await _context.ShippingGovernorates.FindAsync(id);
            if (governorate == null)
            {
                return NotFound("Shipping governorate not found");
            }

            // Check if another governorate has the same name
            var duplicateGovernorate = await _context.ShippingGovernorates
                .FirstOrDefaultAsync(g =>
                    g.Id != id &&
                    (g.EnglishName.ToLower() == dto.EnglishName.ToLower() ||
                     g.ArabicName == dto.ArabicName));

            if (duplicateGovernorate != null)
            {
                return Conflict("Another governorate with this name already exists");
            }

            governorate.EnglishName = dto.EnglishName;
            governorate.ArabicName = dto.ArabicName;
            governorate.Price = dto.Price;
            governorate.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Shipping governorate updated successfully",
                Governorate = new ShippingGovernorateDto
                {
                    Id = governorate.Id,
                    EnglishName = governorate.EnglishName,
                    ArabicName = governorate.ArabicName,
                    Price = governorate.Price
                }
            });
        }

        // DELETE: api/ShippingGovernorates/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteShippingGovernorate(int id)
        {
            var governorate = await _context.ShippingGovernorates.FindAsync(id);
            if (governorate == null)
            {
                return NotFound("Shipping governorate not found");
            }

           

           

            _context.ShippingGovernorates.Remove(governorate);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Shipping governorate deleted successfully" });
        }

        // GET: api/ShippingGovernorates/Summary
        [HttpGet("Summary")]
        public async Task<IActionResult> GetSummary()
        {
            await SeedEgyptianGovernorates();

            var totalGovernorates = await _context.ShippingGovernorates.CountAsync();
            var averagePrice = await _context.ShippingGovernorates.AverageAsync(g => g.Price);
            var minPrice = await _context.ShippingGovernorates.MinAsync(g => g.Price);
            var maxPrice = await _context.ShippingGovernorates.MaxAsync(g => g.Price);

            return Ok(new
            {
                TotalGovernorates = totalGovernorates,
                AveragePrice = Math.Round(averagePrice, 2),
                MinimumPrice = minPrice,
                MaximumPrice = maxPrice
            });
        }
    }
}