using Academic.Models.Dto;
using Digital_Mall_API.Models;
using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.Entities.User___Authentication;
using Digital_Mall_API.Services;
using EmailService;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Restaurant_App.Models.DTO;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

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

        [HttpPost("google-login")]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto dto)
        {
            if (string.IsNullOrEmpty(dto.IdToken))
                return BadRequest("معرف التوكن مطلوب.");

            try
            {
                
                var payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken);

             
                var email = payload.Email;
                var name = payload.Name;
                var googleId = payload.Subject; 

                if (string.IsNullOrEmpty(email))
                    return BadRequest("البريد الإلكتروني غير متوفر من Google.");

               
                var user = await _userManager.FindByEmailAsync(email);
                if (user != null)
                {
                   
                    var logins = await _userManager.GetLoginsAsync(user);
                    var hasGoogleLogin = logins.Any(l => l.LoginProvider == "Google" && l.ProviderKey == googleId);

                    if (!hasGoogleLogin)
                    {
                       
                        var addLoginResult = await _userManager.AddLoginAsync(user, new UserLoginInfo("Google", googleId, "Google"));
                        if (!addLoginResult.Succeeded)
                            return BadRequest("حدث خطأ أثناء ربط حساب Google.");
                    }
                }
                else
                {
                    // إنشاء مستخدم جديد
                    user = new ApplicationUser
                    {
                        UserName = name, 
                        Email = email,
                        DisplayName = name ?? email
                    };

                    var createResult = await _userManager.CreateAsync(user);
                    if (!createResult.Succeeded)
                        return BadRequest(createResult.Errors);

                    await _userManager.AddLoginAsync(user, new UserLoginInfo("Google", googleId, "Google"));

                  
                    if (!await _roleManager.RoleExistsAsync("Customer"))
                        await _roleManager.CreateAsync(new IdentityRole<Guid>("Customer"));
                    await _userManager.AddToRoleAsync(user, "Customer");

                   
                    var customer = new Customer
                    {
                        Id = user.Id.ToString(),
                        UserName = user.UserName,
                        FullName = name ?? email,
                        Email = email,
                        PhoneNumber = "",
                        Password = "",
                        Status = "Active",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Customers.Add(customer);
                    await _context.SaveChangesAsync();
                }

                // إنشاء JWT مخصص للتطبيق
                var token = await GenerateJwt(user);

                return Ok(new
                {
                    token,
                    user = new
                    {
                        user.Id,
                        user.Email,
                        user.DisplayName,
                     
                    }
                });
            }
            catch (InvalidJwtException ex)
            {
                return Unauthorized("توكن Google غير صالح.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"حدث خطأ داخلي: {ex.Message}");
            }
        }
       

        [HttpPost("register-customer")]
        public async Task<IActionResult> RegisterCustomer([FromForm] RegisterCustomerDto dto)
        {
            if (dto.Password != dto.ConfirmPassword)
                return BadRequest("Passwords do not match.");

            var user = new ApplicationUser
            {
                UserName = dto.UserName,
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
                UserName= dto.UserName,
                FullName = dto.FullName,
                PhoneNumber = dto.MobileNumber,
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
                "تفعيل حسابك في TBIGOO",
                GenerateCustomerEmailTemplate(user.DisplayName ?? user.Email, confirmationLink),
                true
            );
            email.SendEmail(message);
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم إنشاء الحساب بنجاح ✅ برجاء تأكيد البريد الإلكتروني قبل تسجيل الدخول عن طريق الرابط المرسل." });
        }

        private string GenerateCustomerEmailTemplate(string customerName, string confirmationLink)
        {
            return $@"
<!DOCTYPE html>
<html dir='rtl' lang='ar'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>تفعيل الحساب - TBIGOO</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
            margin: 0;
            padding: 0;
            background-color: #f4f4f4;
        }}
        .container {{
            max-width: 600px;
            margin: 0 auto;
            background: #ffffff;
            border-radius: 10px;
            overflow: hidden;
            box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
        }}
        .header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 30px 20px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            font-size: 24px;
            font-weight: bold;
        }}
        .content {{
            padding: 30px;
        }}
        .welcome-text {{
            font-size: 18px;
            color: #2d3748;
            margin-bottom: 20px;
            text-align: center;
        }}
        .customer-name {{
            color: #667eea;
            font-weight: bold;
        }}
        .instructions {{
            background: #f7fafc;
            padding: 20px;
            border-radius: 8px;
            border-right: 4px solid #667eea;
            margin: 25px 0;
        }}
        .instructions p {{
            margin: 0 0 15px 0;
            color: #4a5568;
        }}
        .confirmation-button {{
            display: block;
            width: 250px;
            margin: 30px auto;
            padding: 15px 30px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            text-decoration: none;
            border-radius: 25px;
            text-align: center;
            font-weight: bold;
            font-size: 16px;
            transition: all 0.3s ease;
        }}
        .confirmation-button:hover {{
            transform: translateY(-2px);
            box-shadow: 0 6px 12px rgba(102, 126, 234, 0.3);
        }}
        .link-alternative {{
            text-align: center;
            margin: 20px 0;
            color: #718096;
            font-size: 14px;
        }}
        .alternative-link {{
            word-break: break-all;
            background: #edf2f7;
            padding: 10px;
            border-radius: 5px;
            margin: 10px 0;
            font-size: 12px;
            color: #4a5568;
        }}
        .benefits {{
            background: #f0fff4;
            padding: 20px;
            border-radius: 8px;
            border-right: 4px solid #48bb78;
            margin: 25px 0;
        }}
        .benefits h3 {{
            color: #2f855a;
            margin-top: 0;
        }}
        .benefits ul {{
            padding-right: 20px;
            margin: 0;
        }}
        .benefits li {{
            margin-bottom: 10px;
            color: #38a169;
        }}
        .footer {{
            background: #f7fafc;
            padding: 20px;
            text-align: center;
            color: #718096;
            font-size: 14px;
            border-top: 1px solid #e2e8f0;
        }}
        .social-links {{
            margin: 15px 0;
        }}
        .social-links a {{
            color: #667eea;
            text-decoration: none;
            margin: 0 10px;
        }}
        .support-info {{
            background: #fff5f5;
            padding: 15px;
            border-radius: 8px;
            border-right: 4px solid #fc8181;
            margin: 20px 0;
        }}
        .icon {{
            font-size: 24px;
            margin-bottom: 10px;
        }}
        @media (max-width: 600px) {{
            .container {{
                margin: 10px;
            }}
            .content {{
                padding: 20px;
            }}
            .confirmation-button {{
                width: 90%;
            }}
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🛍️ مرحباً بك في TBIGOO</h1>
        </div>
        
        <div class='content'>
            <div class='welcome-text'>
                <div class='icon'>👋</div>
                أهلاً وسهلاً <span class='customer-name'>{customerName}</span>
            </div>
            
            <div class='instructions'>
                <p><strong>خطوات تفعيل الحساب:</strong></p>
                <p>1. انقر على زر تأكيد البريد الإلكتروني أدناه</p>
                <p>2. سيتم تحويلك إلى صفحة التأكيد</p>
                <p>3. يمكنك بعدها تسجيل الدخول والاستمتاع بالتسوق</p>
            </div>
            
            <a href='{confirmationLink}' class='confirmation-button'>
                ✅ تأكيد البريد الإلكتروني
            </a>
            
            <div class='benefits'>
                <h3>🎁 مزايا حسابك في TBIGOO:</h3>
                <ul>
                    <li>تجربة تسوق سلسة وممتعة</li>
                    <li>تتبع الطلبات بسهولة</li>
                    <li>عروض حصرية للمشتركين</li>
                    <li>دعم فني متواصل</li>
                </ul>
            </div>
            
            <div class='link-alternative'>
                <p>إذا لم يعمل الزر أعلاه، يمكنك نسخ الرابط التالي ولصقه في المتصفح:</p>
                <div class='alternative-link'>
                    {confirmationLink}
                </div>
            </div>
            
            <div class='support-info'>
                <p><strong>ملاحظة هامة:</strong></p>
                <p>يجب تأكيد بريدك الإلكتروني قبل أن تتمكن من تسجيل الدخول إلى حسابك.</p>
                <p>إذا واجهتك أي مشكلة، لا تتردد في التواصل مع فريق الدعم.</p>
            </div>
        </div>
        
        <div class='footer'>
            <p><strong>فريق TBIGOO</strong></p>
            <div class='social-links'>
                <a href='https://tbigoo.com/'>الموقع الإلكتروني</a>
            </div>
            <p>© {DateTime.Now.Year} TBIGOO. جميع الحقوق محفوظة.</p>
        </div>
    </div>
</body>
</html>";
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
                PhoneNumber = dto.PhoneNumber,
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
                "تفعيل حسابك في موقع TBIGOO",
                GenerateEmailTemplate(dto.ModelName ?? user.Email, confirmationLink),
                true
            );
            email.SendEmail(message);

            _context.FashionModels.Add(model);
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم إنشاء الحساب بنجاح ✅ برجاء تأكيد البريد الإلكتروني قبل تسجيل الدخول." });
        }

        private string GenerateEmailTemplate(string modelName, string confirmationLink)
        {
            return $@"
<!DOCTYPE html>
<html dir='rtl' lang='ar'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>تفعيل الحساب - TBIGOO</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
            margin: 0;
            padding: 0;
            background-color: #f4f4f4;
        }}
        .container {{
            max-width: 600px;
            margin: 0 auto;
            background: #ffffff;
            border-radius: 10px;
            overflow: hidden;
            box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
        }}
        .header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 30px 20px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            font-size: 24px;
            font-weight: bold;
        }}
        .content {{
            padding: 30px;
        }}
        .welcome-text {{
            font-size: 18px;
            color: #2d3748;
            margin-bottom: 20px;
            text-align: center;
        }}
        .model-name {{
            color: #667eea;
            font-weight: bold;
        }}
        .instructions {{
            background: #f7fafc;
            padding: 20px;
            border-radius: 8px;
            border-right: 4px solid #667eea;
            margin: 25px 0;
        }}
        .instructions p {{
            margin: 0 0 15px 0;
            color: #4a5568;
        }}
        .confirmation-button {{
            display: block;
            width: 250px;
            margin: 30px auto;
            padding: 15px 30px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            text-decoration: none;
            border-radius: 25px;
            text-align: center;
            font-weight: bold;
            font-size: 16px;
            transition: all 0.3s ease;
        }}
        .confirmation-button:hover {{
            transform: translateY(-2px);
            box-shadow: 0 6px 12px rgba(102, 126, 234, 0.3);
        }}
        .link-alternative {{
            text-align: center;
            margin: 20px 0;
            color: #718096;
            font-size: 14px;
        }}
        .alternative-link {{
            word-break: break-all;
            background: #edf2f7;
            padding: 10px;
            border-radius: 5px;
            margin: 10px 0;
            font-size: 12px;
            color: #4a5568;
        }}
        .footer {{
            background: #f7fafc;
            padding: 20px;
            text-align: center;
            color: #718096;
            font-size: 14px;
            border-top: 1px solid #e2e8f0;
        }}
        .social-links {{
            margin: 15px 0;
        }}
        .social-links a {{
            color: #667eea;
            text-decoration: none;
            margin: 0 10px;
        }}
        .support-info {{
            background: #fff5f5;
            padding: 15px;
            border-radius: 8px;
            border-right: 4px solid #fc8181;
            margin: 20px 0;
        }}
        .icon {{
            font-size: 24px;
            margin-bottom: 10px;
        }}
        @media (max-width: 600px) {{
            .container {{
                margin: 10px;
            }}
            .content {{
                padding: 20px;
            }}
            .confirmation-button {{
                width: 90%;
            }}
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🚀 مرحباً بك في TBIGOO</h1>
        </div>
        
        <div class='content'>
            <div class='welcome-text'>
                <div class='icon'>👋</div>
                أهلاً وسهلاً <span class='model-name'>{modelName}</span>
            </div>
            
            <div class='instructions'>
                <p><strong>خطوات تفعيل الحساب:</strong></p>
                <p>1. انقر على زر تأكيد البريد الإلكتروني أدناه</p>
                <p>2. سيتم تحويلك إلى صفحة التأكيد</p>
                <p>3. يمكنك بعدها تسجيل الدخول إلى حسابك</p>
            </div>
            
            <a href='{confirmationLink}' class='confirmation-button'>
                ✅ تأكيد البريد الإلكتروني
            </a>
            
            <div class='link-alternative'>
                <p>إذا لم يعمل الزر أعلاه، يمكنك نسخ الرابط التالي ولصقه في المتصفح:</p>
                <div class='alternative-link'>
                    {confirmationLink}
                </div>
            </div>
            
            <div class='support-info'>
                <p><strong>ملاحظة هامة:</strong></p>
                <p>يجب تأكيد بريدك الإلكتروني قبل أن تتمكن من تسجيل الدخول إلى حسابك.</p>
                <p>إذا واجهتك أي مشكلة، لا تتردد في التواصل مع فريق الدعم.</p>
            </div>
        </div>
        
        <div class='footer'>
            <p><strong>TBIGOO Team</strong></p>
            <div class='social-links'>
                <a href='https://tbigoo.com/'>الموقع الإلكتروني</a>
            </div>
            <p>© {DateTime.Now.Year} TBIGOO. جميع الحقوق محفوظة.</p>
        </div>
    </div>
</body>
</html>";
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user is null)
                return Unauthorized("Invalid credentials.");

            // Check if user is a customer or fashion model
            var customer = await _context.Customers.FindAsync(user.Id.ToString());
            var model = await _context.FashionModels.FindAsync(user.Id.ToString());

            // Apply email confirmation check only for customers and fashion models
            if ((customer != null || model != null) && !user.EmailConfirmed)
                return Unauthorized("يرجى تفعيل البريد الإلكتروني أولاً 🔒");

            var brand = await _context.Brands.FindAsync(user.Id.ToString());
            if (brand != null && brand.Status != "Active")
                return Unauthorized("Your account is not yet approved by admin.");

            if (model != null && model.Status != "Active")
                return Unauthorized("Your account is not yet approved by admin.");

            var designer = await _context.TshirtDesigners.FindAsync(user.Id.ToString());
            if (designer != null && designer.Status != "Active") // Fixed: should check designer, not model
                return Unauthorized("Your account is not yet approved by admin.");

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
                return Content(GenerateErrorHtml("المستخدم غير موجود", "لم نتمكن من العثور على المستخدم المطلوب."), "text/html");

            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

            if (!result.Succeeded)
                return Content(GenerateErrorHtml("فشل تأكيد البريد الإلكتروني", "الرابط غير صالح أو منتهي الصلاحية."), "text/html");

            return Content(GenerateSuccessHtml(user.UserName), "text/html");
        }

        private string GenerateSuccessHtml(string userName)
        {
            return $@"
<!DOCTYPE html>
<html dir='rtl' lang='ar'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>تم تأكيد البريد الإلكتروني - TBIGOO</title>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}
        
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #000000 0%, #2d2d2d 100%);
            min-height: 100vh;
            display: flex;
            justify-content: center;
            align-items: center;
            padding: 20px;
        }}
        
        .container {{
            background: #ffffff;
            border-radius: 20px;
            box-shadow: 0 15px 35px rgba(0, 0, 0, 0.3);
            overflow: hidden;
            max-width: 500px;
            width: 100%;
            text-align: center;
            animation: slideUp 0.6s ease-out;
        }}
        
        .header {{
            background: #000000;
            padding: 40px 30px;
            color: white;
        }}
        
        .success-icon {{
            font-size: 80px;
            margin-bottom: 20px;
            animation: bounce 1s ease-in-out;
        }}
        
        .title {{
            font-size: 28px;
            font-weight: bold;
            margin-bottom: 10px;
            color: white;
        }}
        
        .subtitle {{
            font-size: 16px;
            opacity: 0.9;
            color: #e0e0e0;
        }}
        
        .content {{
            padding: 40px 30px;
        }}
        
        .user-name {{
            color: #000000;
            font-weight: bold;
            font-size: 20px;
            margin-bottom: 15px;
        }}
        
        .message {{
            color: #333333;
            font-size: 16px;
            line-height: 1.6;
            margin-bottom: 30px;
        }}
        
        .login-button {{
            display: inline-block;
            background: #000000;
            color: white;
            text-decoration: none;
            padding: 15px 40px;
            border-radius: 30px;
            font-weight: bold;
            font-size: 16px;
            transition: all 0.3s ease;
            border: 2px solid #000000;
            margin: 10px;
        }}
        
        .login-button:hover {{
            background: white;
            color: #000000;
            transform: translateY(-2px);
            box-shadow: 0 8px 20px rgba(0, 0, 0, 0.2);
        }}
        
        .home-button {{
            display: inline-block;
            background: white;
            color: #000000;
            text-decoration: none;
            padding: 15px 40px;
            border-radius: 30px;
            font-weight: bold;
            font-size: 16px;
            transition: all 0.3s ease;
            border: 2px solid #000000;
            margin: 10px;
        }}
        
        .home-button:hover {{
            background: #000000;
            color: white;
            transform: translateY(-2px);
            box-shadow: 0 8px 20px rgba(0, 0, 0, 0.2);
        }}
        
        .features {{
            background: #f8f8f8;
            padding: 30px;
            border-top: 1px solid #e0e0e0;
        }}
        
        .features-title {{
            color: #000000;
            font-size: 18px;
            font-weight: bold;
            margin-bottom: 20px;
        }}
        
        .feature-list {{
            list-style: none;
            text-align: right;
        }}
        
        .feature-list li {{
            padding: 8px 0;
            color: #333333;
            position: relative;
            padding-right: 25px;
        }}
        
        .feature-list li:before {{
            content: '✓';
            position: absolute;
            right: 0;
            color: #000000;
            font-weight: bold;
        }}
        
        .footer {{
            background: #000000;
            padding: 20px;
            color: white;
            font-size: 14px;
        }}
        
        .footer a {{
            color: #ffffff;
            text-decoration: none;
            margin: 0 10px;
        }}
        
        .footer a:hover {{
            text-decoration: underline;
        }}
        
        @keyframes slideUp {{
            from {{
                opacity: 0;
                transform: translateY(30px);
            }}
            to {{
                opacity: 1;
                transform: translateY(0);
            }}
        }}
        
        @keyframes bounce {{
            0%, 20%, 50%, 80%, 100% {{
                transform: translateY(0);
            }}
            40% {{
                transform: translateY(-10px);
            }}
            60% {{
                transform: translateY(-5px);
            }}
        }}
        
        @media (max-width: 480px) {{
            .container {{
                margin: 10px;
            }}
            
            .header {{
                padding: 30px 20px;
            }}
            
            .content {{
                padding: 30px 20px;
            }}
            
            .login-button, .home-button {{
                display: block;
                margin: 10px 0;
            }}
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='success-icon'>🎉</div>
            <h1 class='title'>تم التأكيد بنجاح!</h1>
            <p class='subtitle'>بريدك الإلكتروني مفعل الآن</p>
        </div>
        
        <div class='content'>
            <div class='user-name'>مرحباً {userName}</div>
            <p class='message'>
                تم تأكيد بريدك الإلكتروني بنجاح ✅<br>
                يمكنك الآن تسجيل الدخول إلى حسابك والاستمتاع بتجربة تسوق مميزة
            </p>
            
            <a href='https://tbigoo.com/#/login' class='login-button'>تسجيل الدخول</a>
            <a href='http://tbigoo.com/#' class='home-button'>الصفحة الرئيسية</a>
        </div>
        
        <div class='features'>
            <h3 class='features-title'>🎁 مزايا انتظروك في حسابك:</h3>
            <ul class='feature-list'>
                <li>تجربة تسوق سريعة وآمنة</li>
                <li>تتبع طلباتك في الوقت الحقيقي</li>
                <li>عروض حصرية لأعضائنا</li>
                <li>دعم فني على مدار الساعة</li>
            </ul>
        </div>
        
        <div class='footer'>
            <p>شكراً لانضمامك إلى عائلة TBIGOO</p>
            <div style='margin-top: 10px;'>
                <a href='http://tbigoo.com/#'>الموقع الإلكتروني</a>
            </div>
            <p style='margin-top: 15px; opacity: 0.8;'>© {DateTime.Now.Year} TBIGOO. جميع الحقوق محفوظة</p>
        </div>
    </div>

    <script>
        // Auto redirect to login after 10 seconds
        setTimeout(function() {{
            window.location.href = 'https://tbigoo.com/#/login';
        }}, 10000);
        
        // Show countdown
        let countdown = 10;
        const countdownElement = document.createElement('div');
        countdownElement.style.marginTop = '15px';
        countdownElement.style.color = '#666';
        countdownElement.style.fontSize = '14px';
        document.querySelector('.content').appendChild(countdownElement);
        
        const countdownInterval = setInterval(function() {{
            countdown--;
            countdownElement.textContent = `سيتم تحويلك تلقائياً إلى صفحة تسجيل الدخول خلال 10 ثواني...`;
            
            if (countdown <= 0) {{
                clearInterval(countdownInterval);
            }}
        }}, 1000);
    </script>
</body>
</html>";
        }

        private string GenerateErrorHtml(string title, string message)
        {
            return $@"
<!DOCTYPE html>
<html dir='rtl' lang='ar'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>خطأ في التأكيد - TBIGOO</title>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}
        
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #000000 0%, #2d2d2d 100%);
            min-height: 100vh;
            display: flex;
            justify-content: center;
            align-items: center;
            padding: 20px;
        }}
        
        .container {{
            background: #ffffff;
            border-radius: 20px;
            box-shadow: 0 15px 35px rgba(0, 0, 0, 0.3);
            overflow: hidden;
            max-width: 500px;
            width: 100%;
            text-align: center;
            animation: shake 0.5s ease-in-out;
        }}
        
        .header {{
            background: #000000;
            padding: 40px 30px;
            color: white;
        }}
        
        .error-icon {{
            font-size: 80px;
            margin-bottom: 20px;
        }}
        
        .title {{
            font-size: 28px;
            font-weight: bold;
            margin-bottom: 10px;
            color: white;
        }}
        
        .content {{
            padding: 40px 30px;
        }}
        
        .error-message {{
            color: #333333;
            font-size: 16px;
            line-height: 1.6;
            margin-bottom: 30px;
        }}
        
        .action-button {{
            display: inline-block;
            background: #000000;
            color: white;
            text-decoration: none;
            padding: 15px 40px;
            border-radius: 30px;
            font-weight: bold;
            font-size: 16px;
            transition: all 0.3s ease;
            border: 2px solid #000000;
            margin: 5px;
        }}
        
        .action-button:hover {{
            background: white;
            color: #000000;
            transform: translateY(-2px);
            box-shadow: 0 8px 20px rgba(0, 0, 0, 0.2);
        }}
        
        .support {{
            background: #f8f8f8;
            padding: 25px;
            border-top: 1px solid #e0e0e0;
        }}
        
        .support-title {{
            color: #000000;
            font-size: 16px;
            font-weight: bold;
            margin-bottom: 10px;
        }}
        
        .support-text {{
            color: #666666;
            font-size: 14px;
            line-height: 1.5;
        }}
        
        .footer {{
            background: #000000;
            padding: 20px;
            color: white;
            font-size: 14px;
        }}
        
        @keyframes shake {{
            0%, 100% {{ transform: translateX(0); }}
            25% {{ transform: translateX(-5px); }}
            75% {{ transform: translateX(5px); }}
        }}
        
        @media (max-width: 480px) {{
            .container {{
                margin: 10px;
            }}
            
            .header {{
                padding: 30px 20px;
            }}
            
            .content {{
                padding: 30px 20px;
            }}
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='error-icon'>❌</div>
            <h1 class='title'>{title}</h1>
        </div>
        
        <div class='content'>
            <p class='error-message'>{message}</p>
            
            <a href='https://tbigoo.com/#' class='action-button'>العودة للرئيسية</a>
            <a href='https://tbigoo.com/#/register' class='action-button'>إنشاء حساب جديد</a>
        </div>
        
        <div class='support'>
            <h3 class='support-title'>🛟 تحتاج مساعدة؟</h3>
            <p class='support-text'>
                إذا كنت تواجه مشكلة في تفعيل حسابك، يرجى التواصل مع فريق الدعم الفني
            </p>
        </div>
        
        <div class='footer'>
            <p>فريق الدعم الفني - TBIGOO</p>
        </div>
    </div>
</body>
</html>";
        }
    }


}
