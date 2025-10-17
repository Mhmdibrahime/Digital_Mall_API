using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.DTOs.BrandAdminDTOs.ProductDiscountAndPromoCodeDTOs;
using Digital_Mall_API.Models.Entities.Promotions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Digital_Mall_API.Controllers.BrandAdmin
{
    [Route("Brand/[controller]")]
    [ApiController]
    public class BrandDiscountsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BrandDiscountsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("summary")]
        public async Task<ActionResult<DiscountSummaryDto>> GetDiscountSummary()
        {
            var totalDiscounts = await _context.ProductDiscounts.CountAsync();
            var activeDiscounts = await _context.ProductDiscounts
                .Where(d => d.Status == "Active")
                .CountAsync();
            var inactiveDiscounts = await _context.ProductDiscounts
                .Where(d => d.Status == "Inactive")
                .CountAsync();
            var promoCodes = await _context.PromoCodes
                .CountAsync();

            return new DiscountSummaryDto
            {
                TotalDiscounts = totalDiscounts,
                ActiveDiscounts = activeDiscounts,
                InactiveDiscounts = inactiveDiscounts,
                PromoCodes = promoCodes
            };
        }

        [HttpGet("GetDiscounts")]
        public async Task<ActionResult<IEnumerable<DiscountDto>>> GetDiscounts(
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var brandId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (brandId == null)
            {
                return Unauthorized();
            }
            var brand = await _context.Brands
                .FirstOrDefaultAsync(b => b.Id == brandId);
            if (brand == null)
            {
                return NotFound("Brand not found.");
            }
                var query = _context.ProductDiscounts
                .Include(d => d.Products)
                    .ThenInclude(p => p.Brand)
                    .Where(d => d.BrandId == brandId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                query = query.Where(d => d.Status == status);
            }

            var totalCount = await query.CountAsync();

            var discounts = await query
                .OrderByDescending(d => d.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new DiscountDto
                {
                    Id = d.Id,
                    DiscountValue = d.DiscountValue,
                    Status = d.Status,
                    CreatedAt = d.CreatedAt,
                    UpdatedAt = d.UpdatedAt,
                    Products = d.Products.Select(p => new ProductDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        BrandName = p.Brand.OfficialName,
                        OriginalPrice = p.Price,
                        DiscountedPrice = CalculateDiscountedPrice(p.Price, d.DiscountValue)
                    }).ToList()
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

        [HttpGet("GetDiscount/{id}")]
        public async Task<ActionResult<DiscountDto>> GetDiscount(int id)
        {
            var brandId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (brandId == null)
            {
                return Unauthorized();
            }
            var brand = await _context.Brands
                .FirstOrDefaultAsync(b => b.Id == brandId);
            if (brand == null)
            {
                return NotFound("Brand not found.");
            }
            var discount = await _context.ProductDiscounts
                .Include(d => d.Products)
                    .ThenInclude(p => p.Brand)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (discount == null)
            {
                return NotFound();
            }

            var discountDto = new DiscountDto
            {
                Id = discount.Id,
                DiscountValue = discount.DiscountValue,
                Status = discount.Status,
                CreatedAt = discount.CreatedAt,
                UpdatedAt = discount.UpdatedAt,
                Products = discount.Products.Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    BrandName = p.Brand.OfficialName,
                    OriginalPrice = p.Price,
                    DiscountedPrice = CalculateDiscountedPrice(p.Price, discount.DiscountValue)
                }).ToList()
            };

            return discountDto;
        }

        [HttpPost("CreateDiscount")]
        public async Task<ActionResult<DiscountDto>> CreateDiscount(CreateDiscountDto createDiscountDto)
        {
            var brandId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (brandId == null)
            {
                return Unauthorized();
            }
            var brand = await _context.Brands
                .FirstOrDefaultAsync(b => b.Id == brandId);
            if (brand == null)
            {
                return NotFound("Brand not found.");
            }
            var discount = new ProductDiscount
            {
                DiscountValue = createDiscountDto.DiscountValue,
                Status = createDiscountDto.Status,
                BrandId = brandId,

            };

            _context.ProductDiscounts.Add(discount);
            await _context.SaveChangesAsync();

            if (createDiscountDto.ProductIds != null && createDiscountDto.ProductIds.Any())
            {
                await AddProductsToDiscount(discount.Id, createDiscountDto.ProductIds);
            }

            return CreatedAtAction(nameof(GetDiscount), new { id = discount.Id }, await GetDiscount(discount.Id));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDiscount(int id, UpdateDiscountDto updateDiscountDto)
        {
            var brandId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (brandId == null)
            {
                return Unauthorized();
            }
            var brand = await _context.Brands
                .FirstOrDefaultAsync(b => b.Id == brandId);
            if (brand == null)
            {
                return NotFound("Brand not found.");
            }
            var discount = await _context.ProductDiscounts
                .Include(d => d.Products)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (discount == null)
            {
                return NotFound();
            }

            if (updateDiscountDto.DiscountValue.HasValue)
                discount.DiscountValue = updateDiscountDto.DiscountValue.Value;

            if (!string.IsNullOrEmpty(updateDiscountDto.Status))
                discount.Status = updateDiscountDto.Status;

            discount.UpdatedAt = DateTime.UtcNow;

            if (updateDiscountDto.ProductIds != null)
            {
                await UpdateDiscountProducts(discount.Id, updateDiscountDto.ProductIds);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DiscountExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDiscount(int id)
        {
            var brandId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (brandId == null)
            {
                return Unauthorized();
            }
            var brand = await _context.Brands
                .FirstOrDefaultAsync(b => b.Id == brandId);
            if (brand == null)
            {
                return NotFound("Brand not found.");
            }
            var discount = await _context.ProductDiscounts
                .Include(d => d.Products)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (discount == null)
            {
                return NotFound();
            }

            foreach (var product in discount.Products)
            {
                product.ProductDiscountId = null;
            }

            _context.ProductDiscounts.Remove(discount);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool DiscountExists(int id)
        {
            return _context.ProductDiscounts.Any(e => e.Id == id);
        }

        private async Task AddProductsToDiscount(int discountId, List<int> productIds)
        {
            var productsToUpdate = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            foreach (var product in productsToUpdate)
            {
                product.ProductDiscountId = discountId;
            }

            await _context.SaveChangesAsync();
        }

        private async Task UpdateDiscountProducts(int discountId, List<int> productIds)
        {
            var currentProducts = await _context.Products
                .Where(p => p.ProductDiscountId == discountId)
                .ToListAsync();

            foreach (var product in currentProducts)
            {
                product.ProductDiscountId = null;
            }

            if (productIds.Any())
            {
                await AddProductsToDiscount(discountId, productIds);
            }
        }

        private static decimal CalculateDiscountedPrice(decimal originalPrice, decimal discountValue)
        {
            return originalPrice * (1 - discountValue / 100);
        }
    }
}