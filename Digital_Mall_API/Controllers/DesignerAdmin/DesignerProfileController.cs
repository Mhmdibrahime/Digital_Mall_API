using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.DTOs.DesignerAdminDTOs;
using Digital_Mall_API.Models.Entities.User___Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Digital_Mall_API.Controllers.DesignerAdmin
{
    [Route("Designer/[controller]")]
    [ApiController]
    public class DesignerProfileController : ControllerBase
    {
        private readonly AppDbContext context;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;

        public DesignerProfileController(AppDbContext context, IWebHostEnvironment env, UserManager<ApplicationUser> userManager)
        {
            this.context = context;
            _env = env;
            _userManager = userManager;
        }


        [Authorize(Roles = "Designer")]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromForm] DesignerFullUpdateDto dto)
        {
            var designerId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(designerId))
                return Unauthorized("Invalid token.");

            var designer = await context.TshirtDesigners.FindAsync(designerId);
            var user = await _userManager.FindByIdAsync(designerId);

            if (designer == null || user == null) return NotFound("Designer not found.");


            if (!string.IsNullOrEmpty(dto.FullName))
            {
                designer.FullName = dto.FullName;
                user.UserName = dto.FullName;
            }


            if (!string.IsNullOrEmpty(dto.PhoneNumber))
            {
                user.PhoneNumber = dto.PhoneNumber;
            }


            //if (dto.ProfileImage != null)
            //{
            //    string fileName = Guid.NewGuid() + Path.GetExtension(dto.ProfileImage.FileName);
            //    string filePath = Path.Combine(_env.WebRootPath, "uploads", fileName);
            //    Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            //    using (var stream = new FileStream(filePath, FileMode.Create))
            //    {
            //        await dto.ProfileImage.CopyToAsync(stream);
            //    }

            //    designer.ProfileImageUrl = "/uploads/" + fileName;
            //}


            if (!string.IsNullOrEmpty(dto.NewPassword))
            {


                var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
                if (!result.Succeeded)
                    return BadRequest(result.Errors);
            }


            context.TshirtDesigners.Update(designer);
            await _userManager.UpdateAsync(user);
            await context.SaveChangesAsync();

            return Ok(new { Message = "Profile updated successfully" });
        }
    }
}
