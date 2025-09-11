using Digital_Mall_API.Models.Entities.User___Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Academic.Models.Dto;
using Digital_Mall_API.Models;
using Microsoft.Extensions.Options;
using EmailService;
using Microsoft.AspNetCore.WebUtilities;
using Restaurant_App.Models.DTO;
using Microsoft.AspNetCore.Authorization;

namespace Digital_Mall_API.Controllers.Account
{
    [Route("[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly IConfiguration _config;
        private readonly IOptions<JwtSettings> _jwtSettings;
        private readonly IEmailSender email;

        
        private static readonly HashSet<string> AllowedRoles =
            new(StringComparer.OrdinalIgnoreCase) { "User", "BrandAdmin", "DesignerAdmin", "ModelAdmin", "SuperAdmin" };

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IConfiguration config,
             IOptions<JwtSettings> jwtSettings,
             IEmailSender email)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _config = config;
            _jwtSettings = jwtSettings;
            this.email = email;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto request)
        {
           
            if (string.IsNullOrWhiteSpace(request.Role)) request.Role = "User";
            if (!AllowedRoles.Contains(request.Role)) return BadRequest("Role is not allowed.");

            var user = new ApplicationUser
            {
                DisplayName=request.FirstName+request.LastName,
                UserName = request.Username,
                Email = request.Email,
                PhoneNumber=request.MobileNumber
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
                return BadRequest(createResult.Errors);

            
            if (!await _roleManager.RoleExistsAsync(request.Role))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>(request.Role));
            }

            var roleResult = await _userManager.AddToRoleAsync(user, request.Role);
            if (!roleResult.Succeeded)
                return BadRequest(roleResult.Errors);


            return Ok(new
            {
                message = $"{request.Role} account created successfully.",
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto request)
        {
            
            ApplicationUser user = await _userManager.FindByEmailAsync(request.Email);

            if (user is null)
                return Unauthorized("Invalid credentials.");

            var check = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
            if (!check.Succeeded)
                return Unauthorized("Invalid credentials.");

            var token = await GenerateJwt(user);
            return Ok(token);
        }

        private async Task<object> GenerateJwt(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? ""),
        new Claim(JwtRegisteredClaimNames.Email, user.Email ?? "")
    };
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Value.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expires = DateTime.UtcNow.AddMinutes(120);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Value.Issuer,
                audience: _jwtSettings.Value.Issuer,
                claims: claims,
                expires: expires,
                signingCredentials: creds);

            return new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                expiresAt = expires
            };
        }

        [HttpPost("ForgetPassword")]
        public async Task<IActionResult> ForgetPassword(ForgetPasswordDto forgetPassword)
        {
            if (!ModelState.IsValid)
                return BadRequest();
            var userpas = await _userManager.FindByEmailAsync(forgetPassword.Email);

            if (userpas is null)
                return BadRequest("in valid");
            var token = await _userManager.GeneratePasswordResetTokenAsync(userpas);
            var param = new Dictionary<string, string?>
            {
                {"token",token },
                {"email" ,forgetPassword.Email }

            };
            var callback = QueryHelpers.AddQueryString(forgetPassword.ClientUri, param);
            var message = new Message([userpas.Email], "Reset password", callback);
            email.SendEmail(message);
            return Ok();


        }

        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto resetPasswordDto)
        {
            if (!ModelState.IsValid)
                return BadRequest();
            var userpas = await _userManager.FindByEmailAsync(resetPasswordDto.Email);

            if (userpas is null)
                return BadRequest("in valid");
            var result = await _userManager.ResetPasswordAsync(userpas, resetPasswordDto.Token!, resetPasswordDto.Password!);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(s => s.Description);
                return BadRequest(new { Errors = errors });
            }
            return Ok();
        }

        [Authorize]
        [HttpGet("TestClaims")]
        public IActionResult TestClaims()
        {
            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            return Ok(claims);
        }
    }


}
