using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.DTOs.SuperAdminDTOs.CommissionDTOs;
using Digital_Mall_API.Models.Entities.Financials;
using Digital_Mall_API.Models.Entities.User___Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Controllers.SuperAdmin
{
    [Route("Super/Financial/[controller]")]
    [ApiController]
    public class CommissionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CommissionsController(AppDbContext context)
        {
            _context = context;
        }

        private async Task<decimal> GetGlobalCommissionRate()
        {
            var globalCommission = await _context.GlobalCommission.FirstOrDefaultAsync();
            if (globalCommission == null)
            {
                globalCommission = new GlobalCommission { CommissionRate = 10.0m };
                _context.GlobalCommission.Add(globalCommission);
                await _context.SaveChangesAsync();
            }
            return globalCommission.CommissionRate;
        }

        private decimal GetEffectiveCommissionRate(decimal? specificRate, decimal globalRate)
        {
            return specificRate ?? globalRate;
        }

        [HttpGet("Global")]
        public async Task<IActionResult> GetGlobalCommission()
        {
            var globalRate = await GetGlobalCommissionRate();
            return Ok(new { CommissionRate = globalRate });
        }

        [HttpPut("Global")]
        public async Task<IActionResult> UpdateGlobalCommission([FromBody] UpdateCommissionRequest request)
        {
            if (request.CommissionRate < 0 || request.CommissionRate > 100)
            {
                return BadRequest("Commission rate must be between 0 and 100");
            }

            var globalCommission = await _context.GlobalCommission.FirstOrDefaultAsync();
            if (globalCommission == null)
            {
                globalCommission = new GlobalCommission();
                _context.GlobalCommission.Add(globalCommission);
            }

            globalCommission.CommissionRate = request.CommissionRate;
           
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Global commission updated successfully",
                CommissionRate = globalCommission.CommissionRate
            });
        }

        [HttpGet("Brands")]
        public async Task<IActionResult> GetBrandsCommissions(
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var globalRate = await GetGlobalCommissionRate();

            var query = _context.Brands.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(b => b.OfficialName.Contains(search));
            }

            var totalCount = await query.CountAsync();

            var brands = await query
                .OrderBy(b => b.OfficialName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BrandCommissionDto
                {
                    Id = b.Id.ToString(),
                    Name = b.OfficialName,
                    EffectiveCommissionRate = GetEffectiveCommissionRate(b.SpecificCommissionRate, globalRate),
                    
                    HasCustomRate = b.SpecificCommissionRate != null
                })
                .ToListAsync();

            return Ok(new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Brands = brands,
                GlobalCommissionRate = globalRate
            });
        }

        [HttpGet("Models")]
        public async Task<IActionResult> GetModelsCommissions(
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var globalRate = await GetGlobalCommissionRate();

            var query = _context.FashionModels.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(m => m.Name.Contains(search) || m.Bio.Contains(search));
            }

            var totalCount = await query.CountAsync();

            var models = await query
                .OrderBy(m => m.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new ModelCommissionDto
                {
                    Id = m.Id.ToString(),
                    Name = m.Name,
                    EffectiveCommissionRate = GetEffectiveCommissionRate(m.SpecificCommissionRate, globalRate),
                   
                    HasCustomRate = m.SpecificCommissionRate != null
                })
                .ToListAsync();

            return Ok(new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Models = models,
                GlobalCommissionRate = globalRate
            });
        }

       
        [HttpPut("UpdateBrandCommission/{id}")]
        public async Task<IActionResult> UpdateBrandCommission(string id, [FromBody] UpdateSpecificCommissionRequest request)
        {
            if (request.CommissionRate < 0 || request.CommissionRate > 100)
            {
                return BadRequest("Commission rate must be between 0 and 100");
            }

            var brand = await _context.Brands.FindAsync(id);
            if (brand == null)
            {
                return NotFound();
            }

            brand.SpecificCommissionRate = request.CommissionRate;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Brand commission updated successfully",
                CommissionRate = brand.SpecificCommissionRate
            });
        }

        [HttpPut("UpdateModelCommission/{id}")]
        public async Task<IActionResult> UpdateModelCommission(string id, [FromBody] UpdateSpecificCommissionRequest request)
        {
            if (request.CommissionRate < 0 || request.CommissionRate > 100)
            {
                return BadRequest("Commission rate must be between 0 and 100");
            }

            var model = await _context.FashionModels.FindAsync(id);
            if (model == null)
            {
                return NotFound();
            }

            model.SpecificCommissionRate = request.CommissionRate;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Model commission updated successfully",
                CommissionRate = model.SpecificCommissionRate
            });
        }

        
       
        
    }
   
}