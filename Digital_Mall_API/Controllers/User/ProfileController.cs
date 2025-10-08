using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.DTOs.SuperAdminDTOs.RefundDTOs;
using Digital_Mall_API.Models.DTOs.UserDTOs.ProfileDTOs;
using Digital_Mall_API.Models.Entities.Orders___Shopping;
using Digital_Mall_API.Models.Entities.User___Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        [HttpGet("following-brands")]
        public async Task<ActionResult<List<FollowingBrandDto>>> GetFollowingBrands()
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

                if (customer == null)
                {
                    return NotFound("Customer not found");
                }

                var followingBrands = await _context.FollowingBrands
                    .Where(fb => fb.CustomerId == customer.Id)
                    .Include(fb => fb.Brand)
                    .Select(fb => new FollowingBrandDto
                    {
                        FollowingId = fb.Id,
                        BrandId = fb.BrandId,
                        BrandName = fb.Brand.OfficialName,
                        LogoUrl = fb.Brand.LogoUrl,
                        FollowersCount = _context.FollowingBrands.Count(f => f.BrandId == fb.BrandId),
                        FollowedAt = fb.FollowedAt
                    })
                    .OrderByDescending(fb => fb.FollowedAt)
                    .ToListAsync();

                return Ok(followingBrands);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving following brands: {ex.Message}");
            }
        }

        [HttpGet("following-models")]
        public async Task<ActionResult<List<FollowingModelDto>>> GetFollowingModels()
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

                if (customer == null)
                {
                    return NotFound("Customer not found");
                }

                var followingModels = await _context.FollowingModels
                    .Where(fm => fm.CustomerId == customer.Id)
                    .Include(fm => fm.FashionModel)
                    .Select(fm => new FollowingModelDto
                    {
                        FollowingId = fm.Id,
                        ModelId = fm.FashionModelId,
                        ModelName = fm.FashionModel.Name,
                        ImageUrl = fm.FashionModel.ImageUrl,
                        FollowersCount = _context.FollowingModels.Count(f => f.FashionModelId == fm.FashionModelId),
                        FollowedAt = fm.FollowedAt
                    })
                    .OrderByDescending(fm => fm.FollowedAt)
                    .ToListAsync();

                return Ok(followingModels);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving following models: {ex.Message}");
            }
        }

        [HttpPost("follow-brand")]
        public async Task<ActionResult> FollowBrand([FromBody] FollowRequestDto request)
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

                if (customer == null)
                {
                    return NotFound("Customer not found");
                }

                var brand = await _context.Brands.FindAsync(request.BrandId);
                if (brand == null)
                {
                    return NotFound("Brand not found");
                }

                var existingFollow = await _context.FollowingBrands
                    .FirstOrDefaultAsync(fb => fb.CustomerId == customer.Id && fb.BrandId == request.BrandId);

                if (existingFollow != null)
                {
                    return BadRequest("Already following this brand");
                }

                var followBrand = new FollowingBrand
                {
                    CustomerId = customer.Id,
                    BrandId = request.BrandId,
                    FollowedAt = DateTime.UtcNow
                };

                _context.FollowingBrands.Add(followBrand);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Brand followed successfully", followingId = followBrand.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error following brand: {ex.Message}");
            }
        }

        [HttpPost("follow-model")]
        public async Task<ActionResult> FollowModel([FromBody] FollowModelRequestDto request)
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

                if (customer == null)
                {
                    return NotFound("Customer not found");
                }

                var model = await _context.FashionModels.FindAsync(request.ModelId);
                if (model == null)
                {
                    return NotFound("Model not found");
                }

                var existingFollow = await _context.FollowingModels
                    .FirstOrDefaultAsync(fm => fm.CustomerId == customer.Id && fm.FashionModelId == request.ModelId);

                if (existingFollow != null)
                {
                    return BadRequest("Already following this model");
                }

                var followModel = new FollowingModel
                {
                    CustomerId = customer.Id,
                    FashionModelId = request.ModelId,
                    FollowedAt = DateTime.UtcNow
                };

                _context.FollowingModels.Add(followModel);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Model followed successfully", followingId = followModel.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error following model: {ex.Message}");
            }
        }

        [HttpDelete("unfollow-brand/{brandId}")]
        public async Task<ActionResult> UnfollowBrand(string brandId)
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

                if (customer == null)
                {
                    return NotFound("Customer not found");
                }

                var followBrand = await _context.FollowingBrands
                    .FirstOrDefaultAsync(fb => fb.CustomerId == customer.Id && fb.BrandId == brandId);

                if (followBrand == null)
                {
                    return NotFound("Not following this brand");
                }

                _context.FollowingBrands.Remove(followBrand);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Brand unfollowed successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error unfollowing brand: {ex.Message}");
            }
        }

        [HttpDelete("unfollow-model/{modelId}")]
        public async Task<ActionResult> UnfollowModel(string modelId)
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

                if (customer == null)
                {
                    return NotFound("Customer not found");
                }

                var followModel = await _context.FollowingModels
                    .FirstOrDefaultAsync(fm => fm.CustomerId == customer.Id && fm.FashionModelId == modelId);

                if (followModel == null)
                {
                    return NotFound("Not following this model");
                }

                _context.FollowingModels.Remove(followModel);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Model unfollowed successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error unfollowing model: {ex.Message}");
            }
        }

        [HttpGet("is-following-brand/{brandId}")]
        public async Task<ActionResult<bool>> IsFollowingBrand(string brandId)
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

                if (customer == null)
                {
                    return NotFound("Customer not found");
                }

                var isFollowing = await _context.FollowingBrands
                    .AnyAsync(fb => fb.CustomerId == customer.Id && fb.BrandId == brandId);

                return Ok(isFollowing);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error checking follow status: {ex.Message}");
            }
        }

        [HttpGet("is-following-model/{modelId}")]
        public async Task<ActionResult<bool>> IsFollowingModel(string modelId)
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

                if (customer == null)
                {
                    return NotFound("Customer not found");
                }

                var isFollowing = await _context.FollowingModels
                    .AnyAsync(fm => fm.CustomerId == customer.Id && fm.FashionModelId == modelId);

                return Ok(isFollowing);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error checking follow status: {ex.Message}");
            }
        }
        [HttpGet("orders")]
        public async Task<ActionResult<OrderHistoryResponse>> GetUserOrders()
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

                if (customer == null)
                {
                    return NotFound("Customer not found");
                }

                var orders = await _context.Orders
                    .Where(o => o.CustomerId == customer.Id)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Brand)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Order)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                var refundRequests = await _context.RefundRequests
                    .Where(r => r.CustomerId == customer.Id)
                    .ToListAsync();

                var orderDtos = new List<UserOrderDto>();

                foreach (var order in orders)
                {
                    var shippingAddress = $"{order.ShippingAddress_Building}, {order.ShippingAddress_Street}, {order.ShippingAddress_City}, {order.ShippingAddress_Country}";

                    var orderDto = new UserOrderDto
                    {
                        OrderId = order.Id,
                        OrderNumber = $"ORD-{order.Id:D3}",
                        OrderDate = order.OrderDate,
                        Status = order.Status,
                        PaymentStatus = order.PaymentStatus,
                        TotalAmount = order.TotalAmount,
                        ItemCount = order.OrderItems?.Count ?? 0,
                        ShippingAddress = shippingAddress,
                        OrderItems = new List<OrderItemDto>()
                    };

                    if (order.OrderItems != null)
                    {
                        foreach (var item in order.OrderItems)
                        {
                            var productVariant = item.ProductVariant;
                            var product = productVariant?.Product;
                            var brand = item.Brand;

                            var itemDto = new OrderItemDto
                            {
                                OrderItemId = item.Id,
                                ProductName = product?.Name ?? "Unknown Product",
                                BrandName = brand?.OfficialName ?? "Unknown Brand",
                                VariantInfo = $"{productVariant?.Size} - {productVariant?.Color}",
                                Quantity = item.Quantity,
                                Price = item.PriceAtTimeOfPurchase,
                                TotalPrice = item.PriceAtTimeOfPurchase * item.Quantity,
                                ImageUrl = product?.Images.FirstOrDefault()?.ImageUrl 
                            };

                            var refundRequest = refundRequests.FirstOrDefault(r =>
                                r.OrderItemId == item.Id && r.OrderId == order.Id);

                            if (refundRequest != null)
                            {
                                itemDto.HasRefundRequest = true;
                                itemDto.RefundStatus = refundRequest.Status;
                                orderDto.HasRefundRequest = true;
                                orderDto.RefundStatus = refundRequest.Status;
                            }

                            orderDto.OrderItems.Add(itemDto);
                        }
                    }

                    orderDtos.Add(orderDto);
                }

                var response = new OrderHistoryResponse
                {
                    Orders = orderDtos,
                    TotalOrders = orders.Count,
                    PendingOrders = orders.Count(o => o.Status == "Pending"),
                    DeliveredOrders = orders.Count(o => o.Status == "Delivered"),
                    CancelledOrders = orders.Count(o => o.Status == "Cancelled")
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving user orders: {ex.Message}");
            }
        }

        [HttpGet("order/{orderId}")]
        public async Task<ActionResult<UserOrderDto>> GetOrderDetails(int orderId)
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

                if (customer == null)
                {
                    return NotFound("Customer not found");
                }

                var order = await _context.Orders
                    .Where(o => o.Id == orderId && o.CustomerId == customer.Id)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Brand)
                    .FirstOrDefaultAsync();

                if (order == null)
                {
                    return NotFound("Order not found");
                }

                var refundRequests = await _context.RefundRequests
                    .Where(r => r.OrderId == orderId && r.CustomerId == customer.Id)
                    .ToListAsync();

                var shippingAddress = $"{order.ShippingAddress_Building}, {order.ShippingAddress_Street}, {order.ShippingAddress_City}, {order.ShippingAddress_Country}";

                var orderDto = new UserOrderDto
                {
                    OrderId = order.Id,
                    OrderNumber = $"ORD-{order.Id:D3}",
                    OrderDate = order.OrderDate,
                    Status = order.Status,
                    PaymentStatus = order.PaymentStatus,
                    TotalAmount = order.TotalAmount,
                    ItemCount = order.OrderItems?.Count ?? 0,
                    ShippingAddress = shippingAddress,
                    OrderItems = new List<OrderItemDto>(),
                    HasRefundRequest = refundRequests.Any(),
                    RefundStatus = refundRequests.FirstOrDefault()?.Status
                };

                if (order.OrderItems != null)
                {
                    foreach (var item in order.OrderItems)
                    {
                        var productVariant = item.ProductVariant;
                        var product = productVariant?.Product;
                        var brand = item.Brand;

                        var itemDto = new OrderItemDto
                        {
                            OrderItemId = item.Id,
                            ProductName = product?.Name ?? "Unknown Product",
                            BrandName = brand?.OfficialName ?? "Unknown Brand",
                            VariantInfo = $"{productVariant?.Size} - {productVariant?.Color}",
                            Quantity = item.Quantity,
                            Price = item.PriceAtTimeOfPurchase,
                            TotalPrice = item.PriceAtTimeOfPurchase * item.Quantity,
                            ImageUrl = product?.Images.FirstOrDefault()?.ImageUrl 
                        };

                        var refundRequest = refundRequests.FirstOrDefault(r => r.OrderItemId == item.Id);
                        if (refundRequest != null)
                        {
                            itemDto.HasRefundRequest = true;
                            itemDto.RefundStatus = refundRequest.Status;
                        }

                        orderDto.OrderItems.Add(itemDto);
                    }
                }

                return Ok(orderDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving order details: {ex.Message}");
            }
        }
        [HttpPost("CreateRefundRequest")]
        public async Task<ActionResult> CreateRefundRequest([FromForm] CreateRefundRequestDto requestDto)
        {
            try
            {
                var orderItem = await _context.OrderItems
                    .Include(oi => oi.Order)
                    .Include(oi => oi.ProductVariant)
                    .ThenInclude(pv => pv.Product)
                    .FirstOrDefaultAsync(oi => oi.Id == requestDto.OrderItemId && oi.OrderId == requestDto.OrderId);

                if (orderItem == null)
                {
                    return BadRequest("Order item not found");
                }

                var existingRefund = await _context.RefundRequests
                    .FirstOrDefaultAsync(r => r.OrderItemId == requestDto.OrderItemId && r.Status != "Rejected");

                if (existingRefund != null)
                {
                    return BadRequest("Refund request already exists for this order item");
                }

                string imageUrl = null;

                if (requestDto.ProductImage != null && requestDto.ProductImage.Length > 0)
                {
                    var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "refunds");
                    if (!Directory.Exists(uploadsPath))
                    {
                        Directory.CreateDirectory(uploadsPath);
                    }

                    var fileName = $"refund_{Guid.NewGuid()}{Path.GetExtension(requestDto.ProductImage.FileName)}";
                    var filePath = Path.Combine(uploadsPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await requestDto.ProductImage.CopyToAsync(stream);
                    }

                    imageUrl = $"/uploads/refunds/{fileName}";
                }

                var refundRequest = new RefundRequest
                {
                    OrderId = requestDto.OrderId,
                    OrderItemId = requestDto.OrderItemId,
                    CustomerId = orderItem.Order.CustomerId,
                    Reason = requestDto.Reason,
                    ImageUrl = imageUrl,
                    Status = "Pending",
                    RequestDate = DateTime.UtcNow
                };

                _context.RefundRequests.Add(refundRequest);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Refund request submitted successfully", refundId = refundRequest.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error creating refund request: {ex.Message}");
            }
        }
    }
}