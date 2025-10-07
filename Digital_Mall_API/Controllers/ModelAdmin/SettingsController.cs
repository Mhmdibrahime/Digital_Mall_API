using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.Entities.User___Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Digital_Mall_API.Controllers.Model
{
    [Route("Model/[controller]")]
    [ApiController]
    public class SettingsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;

        public SettingsController(AppDbContext context, IWebHostEnvironment env, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _env = env;
            _userManager = userManager;
        }

        private string GetCurrentModelId()
        {
            var modelId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return modelId;
        }

        [HttpGet("Profile")]
        public async Task<ActionResult<ProfileDto>> GetProfile()
        {
            var modelId = GetCurrentModelId();
            if (string.IsNullOrEmpty(modelId))
            {
                return Unauthorized("Model not authenticated.");
            }

            var model = await _context.FashionModels
                .FirstOrDefaultAsync(m => m.Id == modelId);

            if (model == null)
            {
                return NotFound("Model profile not found.");
            }

            return new ProfileDto
            {
                Name = model.Name,
                Bio = model.Bio,
                ImageUrl = model.ImageUrl,
                Facebook = model.Facebook,
                Instagram = model.Instgram, 
                OtherSocialAccount = model.OtherSocialAccount
            };
        }

        [HttpPut("UpdateProfile")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult> UpdateProfile([FromForm] UpdateProfileDto dto)
        {
            var modelId = GetCurrentModelId();
            if (string.IsNullOrEmpty(modelId))
            {
                return Unauthorized("Model not authenticated.");
            }

            var model = await _context.FashionModels
                .FirstOrDefaultAsync(m => m.Id == modelId);

            if (model == null)
            {
                return NotFound("Model profile not found.");
            }

            if (!string.IsNullOrEmpty(dto.Name))
            {
                model.Name = dto.Name;
            }

            if (!string.IsNullOrEmpty(dto.Bio))
            {
                model.Bio = dto.Bio;
            }

            if (dto.Image != null)
            {
                var fileName = $"{Guid.NewGuid()}_{dto.Image.FileName}";
                var path = Path.Combine(_env.WebRootPath, "uploads", "profiles", fileName);

                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                using var stream = new FileStream(path, FileMode.Create);
                await dto.Image.CopyToAsync(stream);

                if (!string.IsNullOrEmpty(model.ImageUrl))
                {
                    var oldPath = Path.Combine(_env.WebRootPath, "uploads", "profiles",
                        Path.GetFileName(model.ImageUrl));
                    if (System.IO.File.Exists(oldPath))
                    {
                        System.IO.File.Delete(oldPath);
                    }
                }

                model.ImageUrl = $"/uploads/profiles/{fileName}";
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Profile updated successfully" });
        }

        [HttpPut("UpdateSocialAccounts")]
        public async Task<ActionResult> UpdateSocialAccounts([FromBody] UpdateSocialAccountsDto dto)
        {
            var modelId = GetCurrentModelId();
            if (string.IsNullOrEmpty(modelId))
            {
                return Unauthorized("Model not authenticated.");
            }

            var model = await _context.FashionModels
                .FirstOrDefaultAsync(m => m.Id == modelId);

            if (model == null)
            {
                return NotFound("Model profile not found.");
            }

            model.Facebook = dto.Facebook;
            model.Instgram = dto.Instagram; 
            model.OtherSocialAccount = dto.OtherSocialAccount;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Social accounts updated successfully" });
        }

        //// Endpoint 3: Change Password
        //[HttpPut("ChangePassword")]
        //public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        //{
        //    var modelId = GetCurrentModelId();
        //    if (string.IsNullOrEmpty(modelId))
        //    {
        //        return Unauthorized("Model not authenticated.");
        //    }

        //    var model = await _context.FashionModels
        //        .FirstOrDefaultAsync(m => m.Id == modelId);

        //    if (model == null)
        //    {
        //        return NotFound("Model profile not found.");
        //    }

        //    // Get the ASP.NET Identity user
        //    var user = await _userManager.FindByIdAsync(modelId);
        //    if (user == null)
        //    {
        //        return NotFound("User not found.");
        //    }

        //    // Validate new password and confirmation
        //    if (dto.NewPassword != dto.ConfirmPassword)
        //    {
        //        return BadRequest("New password and confirmation do not match.");
        //    }

        //    if (string.IsNullOrEmpty(dto.NewPassword) || dto.NewPassword.Length < 6)
        //    {
        //        return BadRequest("Password must be at least 6 characters long.");
        //    }

        //    // Change password in ASP.NET Identity
        //    var changePasswordResult = await _userManager.ChangePasswordAsync(
        //        user, dto.CurrentPassword, dto.NewPassword);

        //    if (!changePasswordResult.Succeeded)
        //    {
        //        // If current password is wrong or other identity errors
        //        var errors = string.Join(", ", changePasswordResult.Errors.Select(e => e.Description));
        //        return BadRequest($"Password change failed: {errors}");
        //    }

        //    // Also update password in FashionModel entity (if you're maintaining it separately)
        //    model.Password = dto.NewPassword; // Note: This should be hashed, but Identity handles the hashing

        //    await _context.SaveChangesAsync();

        //    return Ok(new { message = "Password changed successfully" });
        //}

        [HttpPut("ChangePassword")]
        public async Task<ActionResult> ChangePasswordWithoutCurrent([FromBody] ChangePasswordSimpleDto dto)
        {
            var modelId = GetCurrentModelId();
            if (string.IsNullOrEmpty(modelId))
            {
                return Unauthorized("Model not authenticated.");
            }

            var model = await _context.FashionModels
                .FirstOrDefaultAsync(m => m.Id == modelId);

            if (model == null)
            {
                return NotFound("Model profile not found.");
            }

            var user = await _userManager.FindByIdAsync(modelId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            if (dto.NewPassword != dto.ConfirmPassword)
            {
                return BadRequest("New password and confirmation do not match.");
            }

            if (string.IsNullOrEmpty(dto.NewPassword) || dto.NewPassword.Length < 6)
            {
                return BadRequest("Password must be at least 6 characters long.");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);

            if (!resetResult.Succeeded)
            {
                var errors = string.Join(", ", resetResult.Errors.Select(e => e.Description));
                return BadRequest($"Password change failed: {errors}");
            }

            model.Password = dto.NewPassword;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Password changed successfully" });
        }
    }

    public class ProfileDto
    {
        public string Name { get; set; }
        public string Bio { get; set; }
        public string ImageUrl { get; set; }
        public string Facebook { get; set; }
        public string Instagram { get; set; }
        public string OtherSocialAccount { get; set; }
    }

    public class UpdateProfileDto
    {
        public string Name { get; set; }
        public string Bio { get; set; }
        public IFormFile Image { get; set; }
    }

    public class UpdateSocialAccountsDto
    {
        public string Facebook { get; set; }
        public string Instagram { get; set; }
        public string OtherSocialAccount { get; set; }
    }

    public class ChangePasswordDto
    {
        [Required]
        public string CurrentPassword { get; set; }

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; }

        [Required]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public class ChangePasswordSimpleDto
    {
        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; }

        [Required]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }
}