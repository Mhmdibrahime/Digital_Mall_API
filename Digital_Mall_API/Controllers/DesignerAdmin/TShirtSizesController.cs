using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.DTOs.DesignerAdminDTOs;
using Digital_Mall_API.Models.Entities.T_Shirt_Customization;
using Microsoft.AspNetCore.Mvc;

namespace Digital_Mall_API.Controllers.DesignerAdmin
{
    [Route("Designer/[controller]")]
    [ApiController]
    public class TShirtSizesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TShirtSizesController(AppDbContext context)
        {
            _context = context;
        }

      
        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(_context.TShirtSizes.ToList());
        }

      
        [HttpGet("active")]
        public IActionResult GetActive()
        {
            return Ok(_context.TShirtSizes.Where(s => s.IsActive).ToList());
        }

      
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TShirtSizeDto dto)
        {
            var size = new TShirtSize { Name = dto.Name };
            _context.TShirtSizes.Add(size);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Size created successfully" });
        }

        
        [HttpPut("{id}/deactivate")]
        public async Task<IActionResult> Deactivate(int id)
        {
            var size = await _context.TShirtSizes.FindAsync(id);
            if (size == null) return NotFound();

            size.IsActive = false;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Size deactivated" });
        }

       
        [HttpPut("{id}/activate")]
        public async Task<IActionResult> Activate(int id)
        {
            var size = await _context.TShirtSizes.FindAsync(id);
            if (size == null) return NotFound();

            size.IsActive = true;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Size activated" });
        }

       
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var size = await _context.TShirtSizes.FindAsync(id);
            if (size == null) return NotFound();

            _context.TShirtSizes.Remove(size);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Size deleted" });
        }
    }
}

