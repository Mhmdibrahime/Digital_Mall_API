using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.DTOs.DesignerAdminDTOs;
using Digital_Mall_API.Models.Entities.T_Shirt_Customization;
using Microsoft.AspNetCore.Mvc;

namespace Digital_Mall_API.Controllers.DesignerAdmin
{
    [Route("Designer/[controller]")]
    [ApiController]
    public class TShirtStylesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TShirtStylesController(AppDbContext context)
        {
            _context = context;
        }

       
        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(_context.TShirtStyles.ToList());
        }

    
        [HttpGet("active")]
        public IActionResult GetActive()
        {
            return Ok(_context.TShirtStyles.Where(s => s.IsActive).ToList());
        }

        
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TShirtStyleDto dto)
        {
            var style = new TShirtStyle { Name = dto.Name };
            _context.TShirtStyles.Add(style);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Style created successfully" });
        }

        [HttpPut("{id}/deactivate")]
        public async Task<IActionResult> Deactivate(int id)
        {
            var style = await _context.TShirtStyles.FindAsync(id);
            if (style == null) return NotFound();

            style.IsActive = false;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Style deactivated" });
        }

   
        [HttpPut("{id}/activate")]
        public async Task<IActionResult> Activate(int id)
        {
            var style = await _context.TShirtStyles.FindAsync(id);
            if (style == null) return NotFound();

            style.IsActive = true;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Style activated" });
        }

        
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var style = await _context.TShirtStyles.FindAsync(id);
            if (style == null) return NotFound();

            _context.TShirtStyles.Remove(style);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Style deleted" });
        }
    }
}

