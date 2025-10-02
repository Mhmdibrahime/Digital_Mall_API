using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.DTOs.DesignerAdminDTOs;
using Digital_Mall_API.Models.Entities.T_Shirt_Customization;
using Digital_Mall_API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Digital_Mall_API.Controllers.DesignerAdmin
{
    [Route("api/[controller]")]
    [ApiController]
    public class TshirtTemplatesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly FileService _fileService;

        public TshirtTemplatesController(AppDbContext context, FileService fileService)
        {
            _context = context;
            _fileService = fileService;
        }

       
        [HttpGet]
        public IActionResult GetAll()
        {
            var templates = _context.TshirtTemplates
                .Select(t => new TshirtTemplateResponseDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    SizeChartUrl = t.SizeChartUrl,
                    FrontImageUrl = t.FrontImageUrl,
                    BackImageUrl = t.BackImageUrl,
                    LeftImageUrl = t.LeftImageUrl,
                    RightImageUrl = t.RightImageUrl
                })
                .ToList();

            return Ok(templates);
        }

        // POST: api/ProductTemplates
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] TshirtTemplateDto dto)
        {
            var template = new TshirtTemplate
            {
                Name = dto.Name,
                SizeChartUrl = dto.SizeChart != null ? await _fileService.SaveFileAsync(dto.SizeChart, "templates") : null,
                FrontImageUrl = dto.FrontImage != null ? await _fileService.SaveFileAsync(dto.FrontImage, "templates") : null,
                BackImageUrl = dto.BackImage != null ? await _fileService.SaveFileAsync(dto.BackImage, "templates") : null,
                LeftImageUrl = dto.LeftImage != null ? await _fileService.SaveFileAsync(dto.LeftImage, "templates") : null,
                RightImageUrl = dto.RightImage != null ? await _fileService.SaveFileAsync(dto.RightImage, "templates") : null
            };

            _context.TshirtTemplates.Add(template);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Template created successfully", id = template.Id });
        }

       
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromForm] TshirtTemplateDto dto)
        {
            var template = await _context.TshirtTemplates.FindAsync(id);
            if (template == null)
                return NotFound();

            template.Name = dto.Name;

            if (dto.SizeChart != null)
                template.SizeChartUrl = await _fileService.SaveFileAsync(dto.SizeChart, "templates");

            if (dto.FrontImage != null)
                template.FrontImageUrl = await _fileService.SaveFileAsync(dto.FrontImage, "templates");

            if (dto.BackImage != null)
                template.BackImageUrl = await _fileService.SaveFileAsync(dto.BackImage, "templates");

            if (dto.LeftImage != null)
                template.LeftImageUrl = await _fileService.SaveFileAsync(dto.LeftImage, "templates");

            if (dto.RightImage != null)
                template.RightImageUrl = await _fileService.SaveFileAsync(dto.RightImage, "templates");

            await _context.SaveChangesAsync();

            return Ok(new { message = "Template updated successfully" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var template = await _context.TshirtTemplates.FindAsync(id);
            if (template == null)
                return NotFound();

            _context.TshirtTemplates.Remove(template);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Template deleted successfully" });
        }
    }

}

