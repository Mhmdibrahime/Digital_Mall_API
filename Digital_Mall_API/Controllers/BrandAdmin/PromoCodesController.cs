using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.DTOs.BrandAdminDTOs.ProductDiscountAndPromoCodeDTOs;
using Digital_Mall_API.Models.Entities.Promotions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Digital_Mall_API.Controllers.BrandAdmin
{
    [Route("Brand/[controller]")]
    [ApiController]
    public class PromoCodesController : ControllerBase
    {
        private readonly AppDbContext _context;
        public PromoCodesController(AppDbContext context)
        {
            _context = context;
        }
        [HttpGet("GetPromoCodes")]
        public async Task<ActionResult<IEnumerable<PromoCodeDto>>> GetPromoCodes(
    [FromQuery] string? status = null,
    [FromQuery] string? search = null,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20)
        {
            var query = _context.PromoCodes
                .Include(p => p.Usages)
                    .ThenInclude(u => u.Customer)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                query = query.Where(p => p.Status == status);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p =>
                    p.Code.Contains(search) ||
                    p.Name.Contains(search)
                   );
            }

            var totalCount = await query.CountAsync();

            var promoCodes = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PromoCodeDto
                {
                    Id = p.Id,
                    Code = p.Code,
                    Name = p.Name,
                    DiscountValue = p.DiscountValue,
                    Status = p.Status,
                    CurrentUsageCount = p.CurrentUsageCount,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    UsageHistory = p.Usages.Select(u => new PromoCodeUsageDto
                    {
                        Id = u.Id,
                        CustomerName = u.Customer.FullName,
                        CustomerEmail = u.Customer.Email,
                        OrderId = u.OrderId,
                        OrderTotal = u.OrderTotal,
                        DiscountAmount = u.DiscountAmount,
                        UsedAt = u.UsedAt
                    }).ToList()
                })
                .ToListAsync();

            return Ok(new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                PromoCodes = promoCodes
            });
        }

        [HttpGet("GetPromoCode/{id}")]
        public async Task<ActionResult<PromoCodeDto>> GetPromoCode(int id)
        {
            var promoCode = await _context.PromoCodes
                .Include(p => p.Usages)
                    .ThenInclude(u => u.Customer)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (promoCode == null)
            {
                return NotFound();
            }

            var promoCodeDto = new PromoCodeDto
            {
                Id = promoCode.Id,
                Code = promoCode.Code,
                Name = promoCode.Name,
                DiscountValue = promoCode.DiscountValue,
                Status = promoCode.Status,
                CurrentUsageCount = promoCode.CurrentUsageCount,
                StartDate = promoCode.StartDate,
                EndDate = promoCode.EndDate,
                CreatedAt = promoCode.CreatedAt,
                UpdatedAt = promoCode.UpdatedAt,
                UsageHistory = promoCode.Usages.Select(u => new PromoCodeUsageDto
                {
                    Id = u.Id,
                    CustomerName = u.Customer.FullName,
                    CustomerEmail = u.Customer.Email,
                    OrderId = u.OrderId,
                    OrderTotal = u.OrderTotal,
                    DiscountAmount = u.DiscountAmount,
                    UsedAt = u.UsedAt
                }).ToList()
            };

            return promoCodeDto;
        }

        [HttpPost]
        public async Task<ActionResult<PromoCodeDto>> CreatePromoCode(CreatePromoCodeDto createPromoCodeDto)
        {
            if (createPromoCodeDto.StartDate >= createPromoCodeDto.EndDate)
            {
                return BadRequest("End date must be after start date.");
            }

            var existingPromo = await _context.PromoCodes
                .FirstOrDefaultAsync(p => p.Code == createPromoCodeDto.Code);
            if (existingPromo != null)
            {
                return BadRequest("Promo code already exists.");
            }

            var promoCode = new PromoCode
            {
                Code = createPromoCodeDto.Code,
                Name = createPromoCodeDto.Name,
                DiscountValue = createPromoCodeDto.DiscountValue,
                StartDate = createPromoCodeDto.StartDate,
                EndDate = createPromoCodeDto.EndDate,
                Status = createPromoCodeDto.StartDate <= DateTime.UtcNow && createPromoCodeDto.EndDate >= DateTime.UtcNow
                    ? "Active" : "Inactive",
                IsSingleUse = true
            };

            _context.PromoCodes.Add(promoCode);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPromoCode), new { id = promoCode.Id }, await GetPromoCode(promoCode.Id));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePromoCode(int id, UpdatePromoCodeDto updatePromoCodeDto)
        {
            var promoCode = await _context.PromoCodes.FindAsync(id);
            if (promoCode == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(updatePromoCodeDto.Code))
                promoCode.Code = updatePromoCodeDto.Code;

            if (!string.IsNullOrEmpty(updatePromoCodeDto.Name))
                promoCode.Name = updatePromoCodeDto.Name;


            if (updatePromoCodeDto.DiscountValue.HasValue)
                promoCode.DiscountValue = updatePromoCodeDto.DiscountValue.Value;



            if (updatePromoCodeDto.StartDate.HasValue)
                promoCode.StartDate = updatePromoCodeDto.StartDate.Value;

            if (updatePromoCodeDto.EndDate.HasValue)
                promoCode.EndDate = updatePromoCodeDto.EndDate.Value;

            if (!string.IsNullOrEmpty(updatePromoCodeDto.Status))
                promoCode.Status = updatePromoCodeDto.Status;

            promoCode.UpdatedAt = DateTime.UtcNow;

            if (promoCode.StartDate <= DateTime.UtcNow && promoCode.EndDate >= DateTime.UtcNow && promoCode.Status != "Used")
                promoCode.Status = "Active";
            else if (promoCode.EndDate < DateTime.UtcNow)
                promoCode.Status = "Expired";

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PromoCodeExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePromoCode(int id)
        {
            var promoCode = await _context.PromoCodes.FindAsync(id);
            if (promoCode == null)
            {
                return NotFound();
            }

            _context.PromoCodes.Remove(promoCode);
            await _context.SaveChangesAsync();

            return NoContent();
        }





        private bool PromoCodeExists(int id)
        {
            return _context.PromoCodes.Any(e => e.Id == id);
        }
    }
}
