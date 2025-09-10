using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.DTOs.SuperAdminDTOs.BrandsManagementDTOs;
using Digital_Mall_API.Models.Entities.User___Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Controllers.SuperAdmin
{
    [Route("Super/Management/[controller]")]
    [ApiController]
    public class BrandsManagementController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BrandsManagementController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("Summary")]
        public async Task<IActionResult> GetBrandsSummary()
        {
            var totalBrands = await _context.Brands.CountAsync();
            var activeBrands = await _context.Brands.CountAsync(b => b.Status == "Approved");
            var pendingBrands = await _context.Brands.CountAsync(b => b.Status == "Pending");
            var suspendedBrands = await _context.Brands.CountAsync(b => b.Status == "Suspended"); 

            return Ok(new
            {
                TotalBrands = totalBrands,
                ActiveBrands = activeBrands,
                PendingBrands = pendingBrands,
                SuspendedBrands = suspendedBrands
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetBrands(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _context.Brands.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(b =>
                    b.OfficialName.Contains(search) ||
                    (b.Description != null && b.Description.Contains(search)));
            }

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                switch (status.ToLower())
                {
                    case "Active":
                        query = query.Where(b => b.Status == "Approved");
                        break;
                    case "Pending":
                        query = query.Where(b => b.Status == "Pending");
                        break;
                    case "Suspended":
                        query = query.Where(b => b.Status == "Suspended"); 
                        break;
                    case "Rejected":
                        query = query.Where(b => b.Status == "Rejected"); 
                        break;
                }
            }

            var totalCount = await query.CountAsync();

            query = query.OrderBy(b=>b.CreatedAt).OrderDescending();

            var brands = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BrandDto
                {
                    Id = b.Id,
                    OfficialName = b.OfficialName,
                    Description = b.Description,
                    LogoUrl = b.LogoUrl,
                    Status = b.Status,
                    CommissionRate = b.SpecificCommissionRate ?? _context.GlobalCommission.FirstOrDefault().CommissionRate,
                    CreatedAt = b.CreatedAt,
                    ProductsCount = b.Products.Count,
                    OrdersCount = b.Orders.Count(o => o.Status == "completed" || o.Status == "delivered")
                })
                .ToListAsync();

            return Ok(new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Brands = brands
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBrandDetails(string id)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id.ToString() == id);
            if (user == null) return NotFound("Brand not found");
            var brand = await _context.Brands
                .Select(b => new BrandDetailDto
                {
                    Id = b.Id,
                    OfficialName = b.OfficialName,
                    Description = b.Description,
                    Email = user.Email,
                    Phone = user.PhoneNumber,
                    LogoUrl = b.LogoUrl,
                    CommercialRegistrationNumber = b.CommercialRegistrationNumber,
                    TaxCardNumber = b.TaxCardNumber,
                    Status = b.Status,
                    CommissionRate = b.SpecificCommissionRate ?? _context.GlobalCommission.FirstOrDefault().CommissionRate,
                    EvidenceOfProofUrl = b.EvidenceOfProofUrl,
                    CreatedAt = b.CreatedAt,
                    ProductsCount = b.Products.Count,
                    OrdersCount = b.Orders.Count(o => o.Status == "completed" || o.Status == "delivered"),
                    TotalSales = b.Orders
                        .Where(o => o.Status == "completed" || o.Status == "delivered")
                        .Sum(o => (decimal?)o.TotalAmount) ?? 0m
                })
                .FirstOrDefaultAsync(b => b.Id == id);

            if (brand == null)
            {
                return NotFound();
            }

            return Ok(brand);
        }

        [HttpPut("{id}/UpdateStatus")]
        public async Task<IActionResult> UpdateBrandStatus(Guid id, [FromBody] string status)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null)
            {
                return NotFound();
            }

            brand.Status = status;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Brand status updated successfully" });
        }

        [HttpPut("{id}/EditCommission")]
        public async Task<IActionResult> UpdateCommissionRate(Guid id, [FromBody] decimal commissionRate)
        {
            if (commissionRate < 0 || commissionRate > 100)
            {
                return BadRequest("Commission rate must be between 0 and 100");
            }

            var brand = await _context.Brands.FindAsync(id);
            if (brand == null)
            {
                return NotFound();
            }

            brand.SpecificCommissionRate = commissionRate;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Commission rate updated successfully" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBrand(string id)
        {
            var brand = await _context.Brands.FindAsync(id);
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var hasProducts = await _context.Products.AnyAsync(p => p.BrandId == id);
            var hasOrders = await _context.Orders.AnyAsync(o => o.BrandId == id);

            if (hasProducts || hasOrders)
            {
                return BadRequest("Cannot delete brand with associated products or orders");
            }

            _context.Brands.Remove(brand);
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Brand deleted successfully" });
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
    }

    
    
}