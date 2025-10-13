// Controllers/CheckoutController.cs
using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.DTOs;
using Digital_Mall_API.Models.DTOs.BrandAdminDTOs.ProductDiscountAndPromoCodeDTOs;
using Digital_Mall_API.Models.DTOs.UserDTOs.CheckOutDTOs;
using Digital_Mall_API.Models.Entities.Orders___Shopping;
using Digital_Mall_API.Models.Entities.Product_Catalog;
using Digital_Mall_API.Models.Entities.Promotions;
using Digital_Mall_API.Models.Entities.User___Authentication;
using Digital_Mall_API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Polly;
using System.Security.Claims;

namespace Digital_Mall_API.Controllers
{
    [ApiController]
    [Route("User/[controller]")]
    public class CheckoutController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CheckoutController> _logger;
        private readonly IPaymobService _paymobService;

        public CheckoutController(AppDbContext context,
                                ILogger<CheckoutController> logger,
                                IPaymobService paymobService)
        {
            _context = context;
            _logger = logger;
            _paymobService = paymobService;
        }

        [HttpPost("validate-promo-code")]
        public async Task<ActionResult<PromoCodeValidationResponseDto>> ValidatePromoCode([FromBody] ValidatePromoCodeCheckOutDto request)
        {
            try
            {
                var customerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(customerId))
                {
                    return Unauthorized(new PromoCodeValidationResponseDto
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var promoCode = await _context.PromoCodes
                    .Include(pc => pc.Usages)
                    .FirstOrDefaultAsync(pc => pc.Code == request.PromoCode && pc.Status == "Active");

                if (promoCode == null)
                {
                    return BadRequest(new PromoCodeValidationResponseDto
                    {
                        Success = false,
                        Message = "Invalid promo code"
                    });
                }

                // Check if promo code is expired
                if (DateTime.UtcNow < promoCode.StartDate || DateTime.UtcNow > promoCode.EndDate)
                {
                    return BadRequest(new PromoCodeValidationResponseDto
                    {
                        Success = false,
                        Message = "Promo code is expired"
                    });
                }

                // Check if promo code is single use and already used by this customer
                if (promoCode.IsSingleUse && promoCode.Usages.Any(u => u.CustomerId == customerId))
                {
                    return BadRequest(new PromoCodeValidationResponseDto
                    {
                        Success = false,
                        Message = "Promo code has already been used"
                    });
                }

                

                var (isValid, message, applicableItems) = await ValidatePromoCodeForItems(promoCode, request.OrderItems);
                if (!isValid)
                {
                    return BadRequest(new PromoCodeValidationResponseDto
                    {
                        Success = false,
                        Message = message
                    });
                }

                // Calculate discount for applicable items
                var discountDetails = CalculateDiscountForItems(promoCode, applicableItems);

                return Ok(new PromoCodeValidationResponseDto
                {
                    Success = true,
                    Message = "Promo code applied successfully",
                    PromoCodeId = promoCode.Id,
                    DiscountValue = promoCode.DiscountValue,
                    DiscountAmount = discountDetails.TotalDiscount,
                    OriginalTotal = discountDetails.OriginalTotal,
                    FinalTotal = discountDetails.FinalTotal,
                    ApplicableItems = discountDetails.ItemDiscounts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating promo code");
                return StatusCode(500, new PromoCodeValidationResponseDto
                {
                    Success = false,
                    Message = "An error occurred while validating promo code"
                });
            }
        }

        [HttpPost]
        public async Task<ActionResult<CheckoutResponseDto>> Checkout([FromBody] CheckoutRequestDto request)
        {
            try
            {
                var customerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(customerId))
                {
                    return Unauthorized(new CheckoutResponseDto
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                // Validate and apply promo code if provided
                PromoCode promoCode = null;
                List<OrderItemDiscountDto> itemDiscounts = null;

                if (!string.IsNullOrEmpty(request.PromoCode))
                {
                    var promoValidation = await ValidateAndApplyPromoCode(request.PromoCode, request.OrderItems, customerId);
                    if (!promoValidation.Success)
                    {
                        return BadRequest(new CheckoutResponseDto
                        {
                            Success = false,
                            Message = promoValidation.Message
                        });
                    }

                    promoCode = promoValidation.PromoCode;
                    itemDiscounts = promoValidation.ItemDiscounts;
                }

                var (validationResult, orderItems, totalAmount) = await ValidateAndCalculateOrder(request.OrderItems, itemDiscounts);
                if (!validationResult.Success)
                {
                    return BadRequest(new CheckoutResponseDto
                    {
                        Success = false,
                        Message = validationResult.Message
                    });
                }

                var order = new Order
                {
                    CustomerId = customerId,
                    OrderDate = DateTime.UtcNow,
                    Status = "Pending",
                    TotalAmount = totalAmount,
                    ShippingAddress_Building = request.ShippingAddress_Building,
                    ShippingAddress_Street = request.ShippingAddress_Street,
                    ShippingAddress_City = request.ShippingAddress_City,
                    ShippingAddress_Country = request.ShippingAddress_Country,
                    PaymentMethod_Type = request.PaymentMethod,
                    ShippingTrackingNumber = request.ShippingTrackingNumber ?? _context.Users.FirstOrDefault(x => x.Id.ToString() == customerId)?.PhoneNumber,
                    PaymentStatus = "Pending",
                    Notes = request.Notes,
                    OrderItems = orderItems
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Record promo code usage if applicable
                if (promoCode != null)
                {
                    await RecordPromoCodeUsage(promoCode, customerId, order.Id, totalAmount, itemDiscounts);
                }

                if (request.PaymentMethod.ToLower() == "wallet")
                {
                    return await ProcessWalletPayment(order, customerId);
                }
                else if (request.PaymentMethod.ToLower() == "paymob")
                {
                    return await ProcessPaymobPayment(order);
                }
                else
                {
                    return BadRequest(new CheckoutResponseDto
                    {
                        Success = false,
                        Message = "Invalid payment method"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during checkout process");
                return StatusCode(500, new CheckoutResponseDto
                {
                    Success = false,
                    Message = "An error occurred during checkout"
                });
            }
        }

        private async Task<(bool Success, string Message, PromoCode PromoCode, List<OrderItemDiscountDto> ItemDiscounts)>
            ValidateAndApplyPromoCode(string promoCode, List<OrderItemDto> orderItems, string customerId)
        {
            var promo = await _context.PromoCodes
                .Include(pc => pc.Usages)
                .FirstOrDefaultAsync(pc => pc.Code == promoCode && pc.Status == "Active");

            if (promo == null)
            {
                return (false, "Invalid promo code", null, null);
            }

            // Check expiration
            if (DateTime.UtcNow < promo.StartDate || DateTime.UtcNow > promo.EndDate)
            {
                return (false, "Promo code is expired", null, null);
            }

            // Check usage
            if (promo.IsSingleUse && promo.Usages.Any(u => u.CustomerId == customerId))
            {
                return (false, "Promo code has already been used", null, null);
            }

            // Validate against items
            var (isValid, message, applicableItems) = await ValidatePromoCodeForItems(promo, orderItems);
            if (!isValid)
            {
                return (false, message, null, null);
            }

            // Calculate discounts
            var discountDetails = CalculateDiscountForItems(promo, applicableItems);

            return (true, "Promo code applied successfully", promo, discountDetails.ItemDiscounts);
        }

        private async Task<(bool IsValid, string Message, List<OrderItemDto> ApplicableItems)>
            ValidatePromoCodeForItems(PromoCode promoCode, List<OrderItemDto> orderItems)
        {
            var applicableItems = new List<OrderItemDto>();

            foreach (var item in orderItems)
            {
                var productVariant = await _context.ProductVariants
                    .Include(pv => pv.Product)
                    .FirstOrDefaultAsync(pv => pv.Id == item.ProductVariantId);

                if (productVariant == null)
                    continue;

                // Check if promo code is brand-specific
                if (!string.IsNullOrEmpty(promoCode.BrandId) && productVariant.Product.BrandId != promoCode.BrandId)
                    continue;

                // Add item to applicable items
                applicableItems.Add(item);
            }

            if (!applicableItems.Any())
            {
                return (false, "Promo code not applicable to any items in your order", null);
            }

            return (true, "Valid", applicableItems);
        }

        private (decimal TotalDiscount, decimal OriginalTotal, decimal FinalTotal, List<OrderItemDiscountDto> ItemDiscounts)
            CalculateDiscountForItems(PromoCode promoCode, List<OrderItemDto> applicableItems)
        {
            decimal totalDiscount = 0;
            decimal originalTotal = 0;
            var itemDiscounts = new List<OrderItemDiscountDto>();

            foreach (var item in applicableItems)
            {
                var productVariant = _context.ProductVariants
                    .Include(pv => pv.Product)
                    .First(pv => pv.Id == item.ProductVariantId);

                var itemOriginalPrice = productVariant.Product.Price * item.Quantity;
                var itemDiscount = itemOriginalPrice * (promoCode.DiscountValue / 100);
                var itemFinalPrice = itemOriginalPrice - itemDiscount;

                originalTotal += itemOriginalPrice;
                totalDiscount += itemDiscount;

                itemDiscounts.Add(new OrderItemDiscountDto
                {
                    ProductVariantId = item.ProductVariantId,
                    Quantity = item.Quantity,
                    OriginalPrice = productVariant.Product.Price,
                    DiscountedPrice = productVariant.Product.Price * (1 - promoCode.DiscountValue / 100),
                    DiscountAmount = itemDiscount
                });
            }

            return (totalDiscount, originalTotal, originalTotal - totalDiscount, itemDiscounts);
        }

        private async Task RecordPromoCodeUsage(PromoCode promoCode, string customerId, int orderId, decimal orderTotal, List<OrderItemDiscountDto> itemDiscounts)
        {
            var totalDiscount = itemDiscounts?.Sum(x => x.DiscountAmount) ?? 0;

            var usage = new PromoCodeUsage
            {
                PromoCodeId = promoCode.Id,
                CustomerId = customerId,
                OrderId = orderId,
                DiscountAmount = totalDiscount,
                OrderTotal = orderTotal
            };

            // Update promo code usage count
            promoCode.CurrentUsageCount++;
           

            _context.PromoCodeUsages.Add(usage);
            await _context.SaveChangesAsync();
        }

        private async Task<(ValidationResult Success, List<OrderItem> OrderItems, decimal TotalAmount)>
            ValidateAndCalculateOrder(List<OrderItemDto> orderItemsDto, List<OrderItemDiscountDto> itemDiscounts = null)
        {
            var orderItems = new List<OrderItem>();
            decimal totalAmount = 0;

            foreach (var itemDto in orderItemsDto)
            {
                var productVariant = await _context.ProductVariants
                    .Include(pv => pv.Product)
                        .ThenInclude(p => p.ProductDiscount)
                    .Include(pv => pv.Product)
                        .ThenInclude(p => p.Brand)
                    .FirstOrDefaultAsync(pv => pv.Id == itemDto.ProductVariantId);

                if (productVariant == null)
                {
                    return (new ValidationResult(false, $"Product variant with ID {itemDto.ProductVariantId} not found"), null, 0);
                }

                if (productVariant.StockQuantity < itemDto.Quantity)
                {
                    return (new ValidationResult(false,
                        $"Insufficient stock for {productVariant.Product.Name}. Available: {productVariant.StockQuantity}"), null, 0);
                }

                // Calculate base price with product discount
                decimal price = productVariant.Product.Price;
                if (productVariant.Product.ProductDiscount != null &&
                    productVariant.Product.ProductDiscount.Status == "Active")
                {
                    price = price * (1 - productVariant.Product.ProductDiscount.DiscountValue / 100);
                }

                // Apply promo code discount if applicable
                var itemDiscount = itemDiscounts?.FirstOrDefault(id => id.ProductVariantId == itemDto.ProductVariantId);
                if (itemDiscount != null)
                {
                    price = itemDiscount.DiscountedPrice;
                }

                var orderItem = new OrderItem
                {
                    ProductVariantId = itemDto.ProductVariantId,
                    Quantity = itemDto.Quantity,
                    PriceAtTimeOfPurchase = price,
                    BrandId = productVariant.Product.BrandId
                };

                orderItems.Add(orderItem);
                totalAmount += price * itemDto.Quantity;
            }

            return (new ValidationResult(true, "Validation successful"), orderItems, totalAmount);
        }

        private async Task<ActionResult<CheckoutResponseDto>> ProcessWalletPayment(Order order, string customerId)
        {
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null)
            {
                return BadRequest(new CheckoutResponseDto
                {
                    Success = false,
                    Message = "Customer not found"
                });
            }

            if (customer.WalletBalance < order.TotalAmount)
            {
                return BadRequest(new CheckoutResponseDto
                {
                    Success = false,
                    Message = "Insufficient wallet balance"
                });
            }

            customer.WalletBalance -= order.TotalAmount;

            order.PaymentStatus = "Paid";

            foreach (var orderItem in order.OrderItems)
            {
                var productVariant = await _context.ProductVariants.FindAsync(orderItem.ProductVariantId);
                if (productVariant != null)
                {
                    productVariant.StockQuantity -= orderItem.Quantity;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new CheckoutResponseDto
            {
                Success = true,
                Message = "Payment successful using wallet",
                OrderId = order.Id,
                TotalAmount = order.TotalAmount
            });
        }

        private async Task<ActionResult<CheckoutResponseDto>> ProcessPaymobPayment(Order order)
        {
            try
            {
                var paymentResult = await _paymobService.InitializePayment(order);

                if (paymentResult.Success)
                {
                    return Ok(new CheckoutResponseDto
                    {
                        Success = true,
                        Message = "Paymob payment initialized",
                        OrderId = order.Id,
                        TotalAmount = order.TotalAmount,
                        PaymentUrl = paymentResult.PaymentUrl,
                        TransactionId = paymentResult.TransactionId
                    });
                }
                else
                {
                    order.PaymentStatus = "Failed";
                    await _context.SaveChangesAsync();

                    return BadRequest(new CheckoutResponseDto
                    {
                        Success = false,
                        Message = paymentResult.Message
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing Paymob payment");

                order.PaymentStatus = "Failed";
                await _context.SaveChangesAsync();

                return StatusCode(500, new CheckoutResponseDto
                {
                    Success = false,
                    Message = "Error initializing payment"
                });
            }
        }

        [HttpPost("paymob/callback")]
        public async Task<ActionResult<PaymentResultDto>> PaymobCallback([FromBody] PaymobCallbackDto callbackData)
        {
            try
            {
                var result = await _paymobService.HandleCallback(callbackData);

                if (result.Success)
                {
                    var order = await _context.Orders
                        .Include(o => o.OrderItems)
                        .FirstOrDefaultAsync(o => o.Id == result.OrderId);

                    if (order != null)
                    {
                        order.PaymentStatus = "Paid";

                        foreach (var orderItem in order.OrderItems)
                        {
                            var productVariant = await _context.ProductVariants.FindAsync(orderItem.ProductVariantId);
                            if (productVariant != null)
                            {
                                productVariant.StockQuantity -= orderItem.Quantity;
                            }
                        }

                        await _context.SaveChangesAsync();
                    }
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Paymob callback");
                return StatusCode(500, new PaymentResultDto
                {
                    Success = false,
                    Message = "Error processing payment callback"
                });
            }
        }

        private class ValidationResult
        {
            public bool Success { get; }
            public string Message { get; }

            public ValidationResult(bool success, string message)
            {
                Success = success;
                Message = message;
            }
        }
    }
}