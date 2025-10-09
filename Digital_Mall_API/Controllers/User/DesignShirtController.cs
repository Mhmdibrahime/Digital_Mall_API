using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.DTOs.DesignerAdminDTOs;
using Digital_Mall_API.Models.DTOs.UserDTOs;
using Digital_Mall_API.Models.Entities.T_Shirt_Customization;
using Digital_Mall_API.Models.Entities.User___Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Digital_Mall_API.Controllers.User
{
    [Route("User/[controller]")]
    [ApiController]
    public class DesignShirtController : ControllerBase
    {
        private readonly AppDbContext context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DesignShirtController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            this.context = context;
            _userManager = userManager;
        }
        [HttpGet("GetTemplates")]
        public IActionResult GetAll()
        {
            var templates = context.TshirtTemplates
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

        [HttpGet("GetTemplate/{id}")]
        public IActionResult GetById(int id)
        {
            var template = context.TshirtTemplates
                .Where(t => t.Id == id)
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
                .FirstOrDefault();

            if (template == null)
                return NotFound($"Template with ID {id} not found.");

            return Ok(template);
        }

        [HttpGet("active/Size")]
        public IActionResult GetActiveSizes()
        {
            return Ok(context.TShirtSizes.Where(s => s.IsActive).ToList());
        }

        [HttpGet("active/Style")]
        public IActionResult GetActiveStyles()
        {
            return Ok(context.TShirtStyles.Where(s => s.IsActive).ToList());
        }


        [HttpPost("add-order")]
        public async Task<IActionResult> AddOrder([FromForm] AddTshirtDesignOrderDto dto,
     [FromQuery] string? textsJson)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (userId == null)
                    return Unauthorized("User is not logged in.");

                // 🧩 التحقق من القيم الأساسية المطلوبة
                if (string.IsNullOrWhiteSpace(dto.ChosenColor) ||
                    string.IsNullOrWhiteSpace(dto.ChosenStyle) ||
                    string.IsNullOrWhiteSpace(dto.ChosenSize))
                {
                    return BadRequest("Missing required T-shirt customization details (Color, Style, Size).");
                }

                // 🧰 دالة مساعدة لحفظ الملفات
                string SaveFile(IFormFile file, string folder)
                {
                    if (file == null) return null;

                    try
                    {
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

                        if (!allowedExtensions.Contains(ext))
                            throw new InvalidOperationException("Only image files (.jpg, .png, .webp) are allowed.");

                        var uploadsFolder = Path.Combine("wwwroot/uploads", folder);
                        Directory.CreateDirectory(uploadsFolder);

                        var uniqueName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                        var filePath = Path.Combine(uploadsFolder, uniqueName);

                        using var stream = new FileStream(filePath, FileMode.Create);
                        file.CopyTo(stream);

                        return $"/uploads/{folder}/{uniqueName}";
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ File save error: {ex.Message}");
                        return null;
                    }
                }

                var order = new TshirtDesignOrder
                {
                    CustomerUserId = userId,
                    ChosenColor = dto.ChosenColor.Trim(),
                    ChosenStyle = dto.ChosenStyle.Trim(),
                    ChosenSize = dto.ChosenSize.Trim(),
                    TshirtType = string.IsNullOrWhiteSpace(dto.TshirtType) ? "Standard" : dto.TshirtType.Trim(),
                    CustomerDescription = string.IsNullOrWhiteSpace(dto.CustomerDescription)
                        ? "No description provided"
                        : dto.CustomerDescription.Trim(),
                    Length = dto.Length <= 0 ? 1 : dto.Length,
                    Weight = dto.Weight <= 0 ? 1 : dto.Weight,
                    Status = "Pending",
                    IsPaid = false,

                    TshirtFrontImage = SaveFile(dto.TshirtFrontImage, "tshirts") ?? "/uploads/tshirts/default_front.png",
                    TshirtBackImage = SaveFile(dto.TshirtBackImage, "tshirts") ?? "/uploads/tshirts/default_back.png",
                    TshirtLeftImage = SaveFile(dto.TshirtLeftImage, "tshirts") ?? "/uploads/tshirts/default_left.png",
                    TshirtRightImage = SaveFile(dto.TshirtRightImage, "tshirts") ?? "/uploads/tshirts/default_right.png"
                };

                if (dto.CustomerImages != null && dto.CustomerImages.Any())
                {
                    foreach (var file in dto.CustomerImages)
                    {
                        var url = SaveFile(file, "designs");
                        if (!string.IsNullOrEmpty(url))
                            order.Images.Add(new TshirtDesignOrderImage { ImageUrl = url });
                    }
                }

                if (!string.IsNullOrWhiteSpace(textsJson))
                {
                    try
                    {
                        var texts = JsonSerializer.Deserialize<List<AddOrderTextDto>>(textsJson,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (texts != null && texts.Any())
                        {
                            foreach (var text in texts)
                            {
                                order.Texts.Add(new TshirtOrderText
                                {
                                    Text = text.Text,
                                    FontFamily = text.FontFamily,
                                    FontColor = text.FontColor,
                                    FontSize = text.FontSize,
                                    FontStyle = text.FontStyle
                                });
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"❌ JSON Parse error: {ex.Message}");
                        return BadRequest("Invalid JSON format in 'textsJson'. Please send a valid JSON array.");
                    }
                }

                context.TshirtDesignOrders.Add(order);
                await context.SaveChangesAsync();

                return Ok(new
                {
                    message = "✅ Order created successfully.",
                    orderId = order.Id
                });
            }
            catch (InvalidOperationException ex)
            {
                // أخطاء نوع الملف أو validation
                return BadRequest(ex.Message);
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"❌ Database error: {ex.Message}");
                return StatusCode(500, "A database error occurred while saving the order.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Unexpected error: {ex.Message}");
                return StatusCode(500, "An unexpected error occurred while processing your request.");
            }
        }

        [HttpGet("my-orders")]
        public async Task<IActionResult> GetMyOrders()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var orders = await context.TshirtDesignOrders
                .Where(o => o.CustomerUserId == userId)
                .Include(o => o.Texts)
                .Include(o => o.Images)
                .ToListAsync();

            var response = orders.Select(o => new TshirtDesignOrderResponseDto
            {
                Id = o.Id,
                CustomerUserId = o.CustomerUserId,
                ChosenColor = o.ChosenColor,
                ChosenStyle = o.ChosenStyle,
                ChosenSize = o.ChosenSize,
                TshirtType = o.TshirtType,
                Length = o.Length,
                Weight = o.Weight,
                CustomerDescription = o.CustomerDescription,

                // 4 صور التيشيرت
                TshirtFrontImageUrl = o.TshirtFrontImage,
                TshirtBackImageUrl = o.TshirtBackImage,
                TshirtLeftImageUrl = o.TshirtLeftImage,
                TshirtRightImageUrl = o.TshirtRightImage,

                // صور الديزاين كقائمة
                CustomerImageUrls = o.Images.Select(i => i.ImageUrl).ToList(),

                FinalDesignUrl = o.FinalDesignUrl,
                DesignerNotes = o.DesignerNotes,
                Status = o.Status,
                FinalPrice = o.FinalPrice,
                IsPaid = o.IsPaid,
                RequestDate = o.RequestDate,
                Texts = o.Texts.Select(t => new AddOrderTextDto
                {
                    Text = t.Text,
                    FontFamily = t.FontFamily,
                    FontColor = t.FontColor,
                    FontSize = t.FontSize,
                    FontStyle = t.FontStyle
                }).ToList()
            }).ToList();

            return Ok(response);
        }

    }
}
