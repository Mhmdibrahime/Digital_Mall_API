using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.Entities.User___Authentication;
using Digital_Mall_API.Models.DTOs.UserDTOs.ProfileDTOs;
using System.Security.Claims;

namespace Digital_Mall_API.Controllers
{
    [ApiController]
    [Route("User/[controller]")]
    public class ProfileController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProfileController(
            UserManager<ApplicationUser> userManager,
            AppDbContext context,
            IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _context = context;
            _environment = environment;
        }

        [HttpGet("GetProfileInfo")]
        public async Task<ActionResult<ProfileDto>> GetProfile()
        {
            try
            {
                var user = await GetCurrentUser();
                if (user == null)
                {
                    return Unauthorized("User not found");
                }

                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Email == user.Email);

                var followingBrandsCount = await _context.FollowingBrands
                    .CountAsync(fb => fb.CustomerId == customer.Id);

                var followingModelsCount = await _context.FollowingModels
                    .CountAsync(fm => fm.CustomerId == customer.Id);

                var ordersCount = await _context.Orders
                    .CountAsync(o => o.CustomerId == customer.Id);

                var profileDto = new ProfileDto
                {
                    Id = user.Id.ToString(),
                    FullName = customer?.FullName ?? user.DisplayName ?? user.UserName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    ProfilePictureUrl = user.ProfilePictureUrl,
                    CreatedAt = user.CreatedAt,
                    JoiningDate = user.CreatedAt.ToString("MMMM yyyy"),
                    FollowingBrandsCount = followingBrandsCount,
                    FollowingModelsCount = followingModelsCount,
                    OrdersCount = ordersCount
                };

                return Ok(profileDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving profile: {ex.Message}");
            }
        }

        [HttpPut("UpdateProfileInfo")]
        public async Task<ActionResult<ProfileDto>> UpdateProfile([FromBody] UpdateProfileDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var user = await GetCurrentUser();
                if (user == null)
                {
                    return Unauthorized("User not found");
                }

                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Email == user.Email);

                if (!string.IsNullOrEmpty(updateDto.FullName))
                {
                    user.DisplayName = updateDto.FullName;
                }

                if (!string.IsNullOrEmpty(updateDto.PhoneNumber))
                {
                    user.PhoneNumber = updateDto.PhoneNumber;
                }

                if (!string.IsNullOrEmpty(updateDto.Email) && updateDto.Email != user.Email)
                {
                    var existingUser = await _userManager.FindByEmailAsync(updateDto.Email);
                    if (existingUser != null && existingUser.Id != user.Id)
                    {
                        return BadRequest("Email is already taken");
                    }
                    user.Email = updateDto.Email;
                    user.UserName = updateDto.Email;
                }

                if (customer != null)
                {
                    if (!string.IsNullOrEmpty(updateDto.FullName))
                    {
                        customer.FullName = updateDto.FullName;
                    }
                    if (!string.IsNullOrEmpty(updateDto.Email) && updateDto.Email != customer.Email)
                    {
                        customer.Email = updateDto.Email;
                    }
                }

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    return BadRequest(string.Join(", ", result.Errors.Select(e => e.Description)));
                }

                await _context.SaveChangesAsync();

                return await GetProfile();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating profile: {ex.Message}");
            }
        }

        [HttpPost("Update-User-Picture")]
        [Consumes("multipart/form-data")]

        public async Task<ActionResult<ProfileDto>> UploadUserProfilePicture([FromForm] UploadProfilePictureDto model)
        {
            try
            {
                var file = model.File;
                if (file == null || file.Length == 0)
                {
                    return BadRequest("No file uploaded");
                }

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest("Invalid file type. Only JPG, JPEG, PNG, GIF, and WEBP files are allowed.");
                }

                if (file.Length > 5 * 1024 * 1024)
                {
                    return BadRequest("File size too large. Maximum size is 5MB.");
                }

                var user = await GetCurrentUser();
                if (user == null)
                {
                    return Unauthorized("User not found");
                }

                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "profile-pictures");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                var fileName = $"profile_{user.Id}_{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsPath, fileName);

                if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
                {
                    var oldFileName = Path.GetFileName(user.ProfilePictureUrl);
                    var oldFilePath = Path.Combine(uploadsPath, oldFileName);
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                user.ProfilePictureUrl = $"/uploads/profile-pictures/{fileName}";
                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    return BadRequest(string.Join(", ", result.Errors.Select(e => e.Description)));
                }

                return await GetProfile();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error uploading profile picture: {ex.Message}");
            }
        }

        [HttpPost("change-password")]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var user = await GetCurrentUser();
                if (user == null)
                {
                    return Unauthorized("User not found");
                }

                var result = await _userManager.ChangePasswordAsync(
                    user,
                    changePasswordDto.CurrentPassword,
                    changePasswordDto.NewPassword
                );

                if (!result.Succeeded)
                {
                    return BadRequest(string.Join(", ", result.Errors.Select(e => e.Description)));
                }

                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Email == user.Email);

                if (customer != null)
                {
                   
                    customer.Password = changePasswordDto.NewPassword;
                    await _context.SaveChangesAsync();
                }

                return Ok(new { message = "Password changed successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error changing password: {ex.Message}");
            }
        }

        [HttpDelete("picture")]
        public async Task<ActionResult<ProfileDto>> DeleteProfilePicture()
        {
            try
            {
                var user = await GetCurrentUser();
                if (user == null)
                {
                    return Unauthorized("User not found");
                }

                if (string.IsNullOrEmpty(user.ProfilePictureUrl))
                {
                    return BadRequest("No profile picture to delete");
                }

                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "profile-pictures");
                var fileName = Path.GetFileName(user.ProfilePictureUrl);
                var filePath = Path.Combine(uploadsPath, fileName);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                user.ProfilePictureUrl = null;
                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    return BadRequest(string.Join(", ", result.Errors.Select(e => e.Description)));
                }

                return await GetProfile();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deleting profile picture: {ex.Message}");
            }
        }

        private async Task<ApplicationUser> GetCurrentUser()
        {

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return await _userManager.FindByIdAsync(userId);

        }
    }
}