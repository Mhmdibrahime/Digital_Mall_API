using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Digital_Mall_API.Models.Entities.PlatformSettings;
using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.Data;

namespace Digital_Mall_API.Controllers
{
    [ApiController]
    [Route("Super/[controller]")]
    public class PlatformSettingsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public PlatformSettingsController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet]
        public async Task<ActionResult<PlatformSettings>> GetSettings()
        {
            var settings = await _context.PlatformSettings.FirstOrDefaultAsync();

            if (settings == null)
            {
                return Ok(new PlatformSettings
                {
                    Name = "Tbigoo",
                    SupportPhone = null,
                    SupportEmail = null,
                    LogoUrl = null
                });
            }

            return Ok(settings);
        }

        [HttpPost("Update")]
        public async Task<ActionResult<PlatformSettings>> UpdateSettings([FromForm] PlatformSettings model, IFormFile logoFile)
        {
            try
            {
                var existingSettings = await _context.PlatformSettings.FirstOrDefaultAsync();

                if (existingSettings == null)
                {
                    existingSettings = new PlatformSettings();
                    _context.PlatformSettings.Add(existingSettings);
                }

                if (!string.IsNullOrEmpty(model.Name))
                    existingSettings.Name = model.Name;

                if (!string.IsNullOrEmpty(model.SupportEmail))
                    existingSettings.SupportEmail = model.SupportEmail;

                if (!string.IsNullOrEmpty(model.SupportPhone))
                    existingSettings.SupportPhone = model.SupportPhone;

                if (logoFile != null && logoFile.Length > 0)
                {
                    var logoUrl = await SaveLogoFile(logoFile);
                    existingSettings.LogoUrl = logoUrl;
                }

                await _context.SaveChangesAsync();

                return Ok(existingSettings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        private async Task<string> SaveLogoFile(IFormFile logoFile)
        {
            
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".svg" };
            var fileExtension = Path.GetExtension(logoFile.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
            {
                throw new Exception("Invalid file type. Only JPG, JPEG, PNG, GIF, and SVG files are allowed.");
            }

            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "logos");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            var fileName = $"logo_{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await logoFile.CopyToAsync(stream);
            }

            return $"/uploads/logos/{fileName}";
        }
    }
}