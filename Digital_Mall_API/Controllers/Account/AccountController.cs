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
using Digital_Mall_API.Services;
using Digital_Mall_API.Models.Data;

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
        private readonly AppDbContext _context;
        private readonly FileService _fileService;
        private static readonly HashSet<string> AllowedRoles =
            new(StringComparer.OrdinalIgnoreCase) { "User", "Brand", "Designer", "Model", "Super" };

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IConfiguration config,
             IOptions<JwtSettings> jwtSettings,
             IEmailSender email,
            AppDbContext context,
            FileService fileservice)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _config = config;
            _jwtSettings = jwtSettings;
            this.email = email;
            _context = context;
            _fileService = fileservice;
        }

        [HttpPost("register-customer")]
        public async Task<IActionResult> RegisterCustomer([FromForm] RegisterCustomerDto dto)
        {
            if (dto.Password != dto.ConfirmPassword)
                return BadRequest("Passwords do not match.");

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                PhoneNumber = dto.MobileNumber,
                DisplayName = dto.FullName
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded) return BadRequest(result.Errors);

            if (!await _roleManager.RoleExistsAsync("Customer"))
                await _roleManager.CreateAsync(new IdentityRole<Guid>("Customer"));

            await _userManager.AddToRoleAsync(user, "Customer");

            var customer = new Customer
            {
                Id = user.Id.ToString(), 
                FullName = dto.FullName,
                Email = dto.Email,
                Password = dto.Password, 
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            var confirmationLink = $"{Request.Scheme}://{Request.Host}/Account/confirm-email?userId={user.Id}&token={encodedToken}";

            var message = new Message(
                new string[] { user.Email },
                "تفعيل حسابك في Techs Academy",
                $"<h3>مرحباً {user.DisplayName ?? user.Email} 👋</h3>" +
                $"<p>اضغط على الرابط التالي لتفعيل حسابك:</p>" +
                $"<a href='{confirmationLink}'>تأكيد البريد الإلكتروني</a>"
            );
            email.SendEmail(message);
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم إنشاء الحساب بنجاح ✅ برجاء تأكيد البريد الإلكتروني قبل تسجيل الدخول." });
        }

        
        [HttpPost("register-brand")]
        public async Task<IActionResult> RegisterBrand([FromForm] RegisterBrandDto dto)
        {
            if (dto.Password != dto.ConfirmPassword)
                return BadRequest("Passwords do not match.");

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded) return BadRequest(result.Errors);

            if (!await _roleManager.RoleExistsAsync("Brand"))
                await _roleManager.CreateAsync(new IdentityRole<Guid>("Brand"));

            await _userManager.AddToRoleAsync(user, "Brand");

            var brand = new Brand
            {
                Id = user.Id.ToString(), 
                OfficialName = dto.OfficialName,
                Facebook = dto.Facebook,
                Instgram = dto.Instgram,
                Online = dto.Online,
                Ofline = dto.Ofline,
                Location = dto.Location,
                Password = dto.Password, 
                EvidenceOfProofUrl = await _fileService.SaveFileAsync(dto.EvidenceOfProof, "BrandProofs"),
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.Brands.Add(brand);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Brand account created successfully. Waiting for admin approval." });
        }

       
        [HttpPost("register-model")]
        public async Task<IActionResult> RegisterModel([FromForm] RegisterModelDto dto)
        {
            if (dto.Password != dto.ConfirmPassword)
                return BadRequest("Passwords do not match.");

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded) return BadRequest(result.Errors);

            if (!await _roleManager.RoleExistsAsync("FashionModel"))
                await _roleManager.CreateAsync(new IdentityRole<Guid>("FashionModel"));

            await _userManager.AddToRoleAsync(user, "FashionModel");

            var model = new FashionModel
            {
                Id = user.Id.ToString(), 
                Name = dto.ModelName,
                Password = dto.Password, 
                EvidenceOfProofUrl = await _fileService.SaveFileAsync(dto.PersonalProof, "ModelProofs"),
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            var confirmationLink = $"{Request.Scheme}://{Request.Host}/Account/confirm-email?userId={user.Id}&token={encodedToken}";

            var message = new Message(
                new string[] { user.Email },
                "تفعيل حسابك في موقع عرض الأزياء",
                $"<h3>مرحباً {dto.ModelName ?? user.Email} 👋</h3>" +
                $"<p>اضغط على الرابط التالي لتفعيل حسابك:</p>" +
                $"<a href='{confirmationLink}'>تأكيد البريد الإلكتروني</a>"
            );
            email.SendEmail(message);

            _context.FashionModels.Add(model);
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم إنشاء الحساب بنجاح ✅ برجاء تأكيد البريد الإلكتروني قبل تسجيل الدخول." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user is null)
                return Unauthorized("Invalid credentials.");

            if (!user.EmailConfirmed)
                return Unauthorized("يرجى تفعيل البريد الإلكتروني أولاً 🔒");

            var brand = await _context.Brands.FindAsync(user.Id.ToString());
            if (brand != null && brand.Status != "Active")
                return Unauthorized("Your account is not yet approved by admin.");

            
            var model = await _context.FashionModels.FindAsync(user.Id.ToString());
            if (model != null && model.Status != "Active")
                return Unauthorized("Your account is not yet approved by admin.");


            var designer = await _context.TshirtDesigners.FindAsync(user.Id.ToString());
            if (model != null && model.Status != "Active")
                return Unauthorized("Your account is not yet approved by admin.");

            var customer = await _context.Customers.FindAsync(user.Id.ToString());
            if (customer != null && customer.Status != "Active")
                return Unauthorized("Your account is not active.");

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

            var expires = DateTime.UtcNow.AddDays(7);

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
        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmCustomerEmail(Guid userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return BadRequest("User not found.");

            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

            if (!result.Succeeded)
                return BadRequest("Email confirmation failed.");

           
          

            return Ok("تم تأكيد البريد الإلكتروني بنجاح 🎉 يمكنك الآن تسجيل الدخول.");
        }
    }


}
