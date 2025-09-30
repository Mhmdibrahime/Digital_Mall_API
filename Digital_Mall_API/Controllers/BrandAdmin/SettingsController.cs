using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.Entities.User___Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Digital_Mall_API.Models.DTOs.BrandAdminDTOs.SettingsDTOs;
using System.IO;
namespace Digital_Mall_API.Controllers.BrandAdmin
{
    [Route("Brand/[controller]")]
    [ApiController]
    public class SettingsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SettingsController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private string GetCurrentBrandId()
        {
            var brandId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return brandId;
        }

        [HttpGet("Profile")]
        public async Task<ActionResult<BrandProfileDto>> GetBrandProfile()
        {
            var brandId = GetCurrentBrandId();
            if (string.IsNullOrEmpty(brandId))
            {
                return Unauthorized("Brand not authenticated.");
            }
            var user = await _userManager.FindByIdAsync(brandId);
            var brand = await _context.Brands
                .Where(b => b.Id.ToString() == brandId)
                .Select(b => new BrandProfileDto
                {
                    BrandName = b.OfficialName,
                    LogoUrl = b.LogoUrl ?? "No Logo Yet",
                    ContactEmail = user.Email, 
                    AboutUs = b.Description, 
                    ReturnPolicy = b.ReturnPolicy
                })
                .FirstOrDefaultAsync();

            if (brand == null)
            {
                return NotFound("Brand not found.");
            }

            return Ok(brand);
        }

        [HttpPut("UpdateProfile")]
        public async Task<IActionResult> UpdateBrandProfile([FromBody] UpdateBrandProfileDto updateDto)
        {
            var brandId = GetCurrentBrandId();
            if (string.IsNullOrEmpty(brandId))
            {
                return Unauthorized("Brand not authenticated.");
            }

            var brand = await _context.Brands.FindAsync(brandId);
            if (brand == null)
            {
                return NotFound("Brand not found.");
            }

            var user = await _userManager.FindByIdAsync(brandId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            if (!string.IsNullOrEmpty(updateDto.BrandName))
                brand.OfficialName = updateDto.BrandName;

            if (!string.IsNullOrEmpty(updateDto.Description))
                brand.Description = updateDto.Description;

            if (!string.IsNullOrEmpty(updateDto.ReturnPolicy))
                brand.ReturnPolicy = updateDto.ReturnPolicy;

            //if (!string.IsNullOrEmpty(updateDto.Email) && updateDto.Email != user.Email)
            //{
            //    if (!IsValidEmail(updateDto.Email))
            //    {
            //        return BadRequest("Invalid email format.");
            //    }

            //    var existingUser = await _userManager.FindByEmailAsync(updateDto.Email);
            //    if (existingUser != null && existingUser.Id != user.Id)
            //    {
            //        return BadRequest("Email is already taken by another user.");
            //    }

            //    var setEmailResult = await _userManager.SetEmailAsync(user, updateDto.Email);
            //    if (!setEmailResult.Succeeded)
            //    {
            //        return BadRequest(string.Join(", ", setEmailResult.Errors.Select(e => e.Description)));
            //    }

            //    var setUserNameResult = await _userManager.SetUserNameAsync(user, updateDto.Email);
            //    if (!setUserNameResult.Succeeded)
            //    {
            //        return BadRequest(string.Join(", ", setUserNameResult.Errors.Select(e => e.Description)));
            //    }
            //}

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Brand profile updated successfully." });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, "An error occurred while updating the profile.");
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        [HttpPost("UploadLogo")]
        public async Task<ActionResult<string>> UploadLogo(IFormFile file)
        {
            var brandId = GetCurrentBrandId();
            if (string.IsNullOrEmpty(brandId))
            {
                return Unauthorized("Brand not authenticated.");
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest("Invalid file type. Only JPG, JPEG, PNG, and GIF are allowed.");
            }

            if (file.Length > 5 * 1024 * 1024)
            {
                return BadRequest("File size too large. Maximum size is 5MB.");
            }

            try
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "brands");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = $"{brandId}_{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, fileName);
                var fileUrl = $"/uploads/brands/{fileName}";

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var brand = await _context.Brands.FindAsync(brandId);
                if (brand != null)
                {
                    if (!string.IsNullOrEmpty(brand.LogoUrl) && brand.LogoUrl.StartsWith("/uploads/brands/"))
                    {
                        var oldFileName = Path.GetFileName(brand.LogoUrl);
                        var oldFilePath = Path.Combine(uploadsFolder, oldFileName);
                        if (System.IO. File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    brand.LogoUrl = fileUrl;
                    await _context.SaveChangesAsync();
                }

                return Ok(new { logoUrl = fileUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error uploading file: {ex.Message}");
            }
        }
        [HttpPut("ChangePassword")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            var brandId = GetCurrentBrandId();
            var brand = await _context.Brands.FindAsync(brandId);
            if (string.IsNullOrEmpty(brandId))
            {
                return Unauthorized("Brand not authenticated.");
            }

            if (string.IsNullOrEmpty(changePasswordDto.NewPassword) ||
                string.IsNullOrEmpty(changePasswordDto.ConfirmPassword))
            {
                return BadRequest("New password and confirmation are required.");
            }

            if (changePasswordDto.NewPassword != changePasswordDto.ConfirmPassword)
            {
                return BadRequest("New password and confirmation do not match.");
            }

            if (changePasswordDto.NewPassword.Length < 6)
            {
                return BadRequest("Password must be at least 6 characters long.");
            }

            try
            {
                var user = await _userManager.FindByIdAsync(brandId);
                if (user == null)
                {
                    return NotFound("User not found.");
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, changePasswordDto.NewPassword);

                if (result.Succeeded)
                {
                    brand.Password = changePasswordDto.NewPassword;
                    _context.SaveChanges();
                    return Ok(new { message = "Password changed successfully." });
                }

                return BadRequest(string.Join(", ", result.Errors.Select(e => e.Description)));
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error changing password: {ex.Message}");
            }
        }

       
    }

}