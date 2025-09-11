using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.DTOs.DesignerAdminDTOs;
using Digital_Mall_API.Models.Entities.T_Shirt_Customization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Digital_Mall_API.Controllers.DesignerAdmin
{
    [Route("Designer/[controller]")]
    [ApiController]
    public class SubmissionController : ControllerBase
    {
        private readonly AppDbContext context;
        private readonly IWebHostEnvironment _env;

        public SubmissionController(AppDbContext context, IWebHostEnvironment env)
        {
            this.context = context;
            _env = env;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromForm] TshirtDesignSubmissionCreateDto dto)
        {
            var order = await context.TshirtDesignOrders
                .Include(o => o.CustomerUser)
                .FirstOrDefaultAsync(o => o.Id == dto.OrderId);

            if (order == null) return NotFound("Order not found.");

            var submission = new TshirtDesignSubmission
            {
                OrderId = dto.OrderId,
                DesignName = dto.DesignName,
                Description = dto.Description,
                SubmissionDate = DateTime.UtcNow,
                Images = new List<TshirtDesignSubmissionImage>()
            };


            if (dto.DesignImages != null && dto.DesignImages.Any())
            {
                foreach (var file in dto.DesignImages)
                {
                    string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                    string filePath = Path.Combine(_env.WebRootPath, "uploads", fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    submission.Images.Add(new TshirtDesignSubmissionImage
                    {
                        ImageUrl = "/uploads/" + fileName
                    });
                }
            }

            context.TshirtDesignSubmissions.Add(submission);
            await context.SaveChangesAsync();

            return Ok(new { Message = "Submission created successfully", submission.Id });
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TshirtDesignSubmissionListDto>>> GetAll(
     [FromQuery] int pageNumber = 1,
     [FromQuery] int pageSize = 20)
        {
            var query = context.TshirtDesignSubmissions
                .Include(s => s.Order).ThenInclude(o => o.CustomerUser)
                .Include(s => s.Images)
                .AsQueryable();


            var totalCount = await query.CountAsync();


            var submissions = await query
                .OrderByDescending(s => s.SubmissionDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = submissions.Select(s => new TshirtDesignSubmissionListDto
            {
                SubmissionId = s.Id,
                ClientName = s.Order.CustomerUser?.FullName ?? "Unknown",
                DesignName = s.DesignName,
                Description = s.Description,
                SubmissionDate = s.SubmissionDate,
                ImageUrls = s.Images.Select(i => i.ImageUrl).ToList()
            });

            return Ok(new
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Data = result
            });
        }

    }
}
