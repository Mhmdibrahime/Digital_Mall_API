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
                    OrderItems = orderItems,
                    PaymobOrderId = ".",
                    TransactionId = "."
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
                    return await ProcessPaymobPayment(order, customerId);
                }
                else if (request.PaymentMethod.ToLower() == "cod")
                {
                    order.PaymentStatus = "Pending";
                    order.Status = "Pending";

                    await _context.SaveChangesAsync();

                    return Ok(new CheckoutResponseDto
                    {
                        Success = true,
                        Message = "Order placed successfully with Cash on Delivery",
                        OrderId = order.Id,
                        TotalAmount = order.TotalAmount
                    });
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

        private async Task<ActionResult<CheckoutResponseDto>> ProcessPaymobPayment(Order order,string customerId)
        {
            try
            {
                var paymentResult = await _paymobService.InitializePayment(order,customerId);

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
        [HttpGet("paymob/callback")] // Add GET support for query parameters
        public async Task<ActionResult> PaymobCallback(
    [FromBody] PaymobCallbackDto? callbackData = null,
    [FromQuery] string? id = null,
    [FromQuery] string? pending = null,
    [FromQuery] long? amount_cents = null,
    [FromQuery] bool? success = null,
    [FromQuery] string? order = null,
    [FromQuery] string? currency = null,
    [FromQuery] bool? error_occured = null,
    [FromQuery] string? data_message = null)
        {
            try
            {
                PaymobCallbackDto processedCallbackData;

                // Handle query parameters (automatic callback from Paymob)
                if (callbackData == null && !string.IsNullOrEmpty(id))
                {
                    await _paymobService.LogToDatabase("Information", "CheckoutController",
                        "Processing callback from QUERY PARAMETERS",
                        $"Order: {order}, Success: {success}", null, order, id);

                    processedCallbackData = new PaymobCallbackDto
                    {
                        id = long.TryParse(id, out var tid) ? tid : 0,
                        pending = bool.TryParse(pending, out var p) && p,
                        amount_cents = amount_cents ?? 0,
                        success = success ?? false,
                        order = order ?? string.Empty,
                        currency = currency ?? "EGP",
                        error_occured = error_occured ?? false,
                        data = new PaymobCallbackData
                        {
                            message = data_message ?? string.Empty
                        }
                    };
                }
                // Handle JSON body (manual testing)
                else if (callbackData != null)
                {
                    await _paymobService.LogToDatabase("Information", "CheckoutController",
                        "Processing callback from JSON BODY",
                        $"Order: {callbackData.order}, Success: {callbackData.success}",
                        null, callbackData.order, callbackData.id.ToString());

                    processedCallbackData = callbackData;
                }
                else
                {
                    await _paymobService.LogToDatabase("Warning", "CheckoutController",
                        "Invalid callback - no data provided", null);

                    return BadRequest(new PaymentResultDto
                    {
                        Success = false,
                        Message = "No callback data provided"
                    });
                }

                _logger.LogInformation("🔄 Paymob Callback Received - Success: {Success}, Paymob Order: {Order}",
                    processedCallbackData.success, processedCallbackData.order);

                var result = await _paymobService.HandleCallback(processedCallbackData);

                if (result.Success)
                {
                    var dbOrder = await _context.Orders
                        .Include(o => o.OrderItems)
                        .FirstOrDefaultAsync(o => o.PaymobOrderId == processedCallbackData.order);

                    if (dbOrder != null)
                    {
                        await _paymobService.LogToDatabase("Information", "CheckoutController",
                            "Found order in database",
                            $"DB Order: {dbOrder.Id}, Paymob Order: {dbOrder.PaymobOrderId}",
                            dbOrder.Id.ToString(), dbOrder.PaymobOrderId);

                        // Only update if not already paid
                        if (dbOrder.PaymentStatus != "Paid")
                        {
                            dbOrder.PaymentStatus = "Paid";
                            dbOrder.TransactionId = processedCallbackData.id.ToString();

                            // Update stock
                            foreach (var orderItem in dbOrder.OrderItems)
                            {
                                var productVariant = await _context.ProductVariants.FindAsync(orderItem.ProductVariantId);
                                if (productVariant != null)
                                {
                                    var oldQuantity = productVariant.StockQuantity;
                                    productVariant.StockQuantity -= orderItem.Quantity;

                                    await _paymobService.LogToDatabase("Information", "CheckoutController",
                                        "Updated stock",
                                        $"Variant: {orderItem.ProductVariantId}, Old: {oldQuantity}, New: {productVariant.StockQuantity}",
                                        dbOrder.Id.ToString(), dbOrder.PaymobOrderId);
                                }
                            }

                            await _context.SaveChangesAsync();

                            await _paymobService.LogToDatabase("Information", "CheckoutController",
                                "Order successfully updated to Paid",
                                $"Order {dbOrder.Id} completed",
                                dbOrder.Id.ToString(), dbOrder.PaymobOrderId);
                        }
                        else
                        {
                            await _paymobService.LogToDatabase("Information", "CheckoutController",
                                "Order already paid - skipping update",
                                $"Order {dbOrder.Id} was already paid",
                                dbOrder.Id.ToString(), dbOrder.PaymobOrderId);
                        }
                    }
                    else
                    {
                        await _paymobService.LogToDatabase("Warning", "CheckoutController",
                            "Order not found in database",
                            $"Paymob Order ID: {processedCallbackData.order}",
                            null, processedCallbackData.order);
                    }
                }
                else
                {
                    await _paymobService.LogToDatabase("Warning", "CheckoutController",
                        "Payment failed in callback",
                        result.Message, null, processedCallbackData.order);
                }

                // Return simple OK response for Paymob
                return Ok(new { success = true, message = "Paymen processed successfully" });
            }
            catch (Exception ex)
            {
                await _paymobService.LogToDatabase("Error", "CheckoutController",
                    "Error processing callback", ex.Message, null, order);

                return StatusCode(500, new { success = false, message = "Error processing callback" });
            }
        }

        // NEW: Callback Test UI Endpoint
        [HttpGet("paymob/callback-test")]
        public IActionResult CallbackTestUI()
        {
            var html = @"
<!DOCTYPE html>
<html>
<head>
    <title>Paymob Callback Tester</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 40px; background: #f5f5f5; }
        .container { max-width: 1000px; margin: 0 auto; background: white; padding: 20px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
        .form-group { margin-bottom: 15px; }
        label { display: block; margin-bottom: 5px; font-weight: bold; }
        input, textarea, select { width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px; }
        button { background: #007bff; color: white; padding: 10px 20px; border: none; border-radius: 4px; cursor: pointer; margin-right: 10px; margin-bottom: 10px; }
        button:hover { background: #0056b3; }
        .button-success { background: #28a745; }
        .button-success:hover { background: #218838; }
        .button-warning { background: #ffc107; color: #212529; }
        .button-warning:hover { background: #e0a800; }
        .result { margin-top: 20px; padding: 15px; border-radius: 5px; }
        .success { background: #d4edda; color: #155724; border: 1px solid #c3e6cb; }
        .error { background: #f8d7da; color: #721c24; border: 1px solid #f5c6cb; }
        .info { background: #d1ecf1; color: #0c5460; border: 1px solid #bee5eb; }
        .logs { margin-top: 20px; }
        .log-entry { padding: 10px; border-bottom: 1px solid #eee; }
        .log-info { background: #d1ecf1; }
        .log-warning { background: #fff3cd; }
        .log-error { background: #f8d7da; }
        .section { margin-bottom: 30px; padding: 20px; border: 1px solid #e9ecef; border-radius: 5px; }
        .section h3 { margin-top: 0; color: #495057; }
        .url-display { background: #f8f9fa; padding: 10px; border-radius: 4px; border: 1px solid #e9ecef; word-break: break-all; font-family: monospace; font-size: 12px; }
        .tab-buttons { display: flex; margin-bottom: 20px; }
        .tab-button { padding: 10px 20px; border: 1px solid #ddd; background: #f8f9fa; cursor: pointer; }
        .tab-button.active { background: #007bff; color: white; border-color: #007bff; }
        .tab-content { display: none; }
        .tab-content.active { display: block; }
    </style>
</head>
<body>
    <div class='container'>
        <h1>🔄 Paymob Callback Tester</h1>
        
        <div class=""tab-buttons"">
            <div class=""tab-button active"" onclick=""showTab('manual')"">Manual Testing</div>
            <div class=""tab-button"" onclick=""showTab('auto')"">Automatic Callback</div>
            <div class=""tab-button"" onclick=""showTab('status')"">Order Status</div>
            <div class=""tab-button"" onclick=""showTab('logs')"">Logs</div>
        </div>

        <!-- Manual Testing Tab -->
        <div id=""manual"" class=""tab-content active"">
            <div class=""section"">
                <h3>📝 Manual Callback Test (JSON Body)</h3>
                
                <div class='form-group'>
                    <label for='paymobOrderId'>Paymob Order ID:</label>
                    <input type='text' id='paymobOrderId' placeholder='Enter Paymob Order ID' value=""413646773"">
                </div>
                
                <div class='form-group'>
                    <label for='transactionId'>Transaction ID:</label>
                    <input type='text' id='transactionId' placeholder='Enter Transaction ID' value=""366427700"">
                </div>
                
                <div class='form-group'>
                    <label for='amount'>Amount (cents):</label>
                    <input type='number' id='amount' value='4400'>
                </div>
                
                <div class='form-group'>
                    <label for='success'>Success:</label>
                    <select id='success'>
                        <option value='true'>True (Approved)</option>
                        <option value='false'>False (Failed)</option>
                    </select>
                </div>
                
                <button onclick='sendManualCallback()'>Send Manual Callback (JSON)</button>
                <button class=""button-warning"" onclick='sendFailedCallback()'>Send Failed Callback</button>
            </div>
        </div>

        <!-- Automatic Callback Tab -->
        <div id=""auto"" class=""tab-content"">
            <div class=""section"">
                <h3>🔗 Automatic Callback Simulation (Query Parameters)</h3>
                
                <div class='form-group'>
                    <label for='autoPaymobOrderId'>Paymob Order ID:</label>
                    <input type='text' id='autoPaymobOrderId' placeholder='Enter Paymob Order ID' value=""413646773"">
                </div>
                
                <div class='form-group'>
                    <label for='autoAmount'>Amount (cents):</label>
                    <input type='number' id='autoAmount' value='4400'>
                </div>
                
                <button class=""button-success"" onclick='generateAutoCallbackUrl()'>Generate Automatic Callback URL</button>
                <button class=""button-success"" onclick='testAutoCallback()'>Test Automatic Callback</button>
                
                <div class='form-group' style=""margin-top: 20px;"">
                    <label>Generated URL:</label>
                    <div id=""generatedUrl"" class=""url-display"">
                        Click ""Generate Automatic Callback URL"" to see the URL
                    </div>
                </div>
            </div>
        </div>

        <!-- Order Status Tab -->
        <div id=""status"" class=""tab-content"">
            <div class=""section"">
                <h3>📊 Order Status Check</h3>
                
                <div class='form-group'>
                    <label for='statusPaymobOrderId'>Paymob Order ID:</label>
                    <input type='text' id='statusPaymobOrderId' placeholder='Enter Paymob Order ID' value=""413646773"">
                </div>
                
                <button onclick='checkOrderStatus()'>Check Order Status</button>
                <button onclick='loadAllOrders()'>Load All Orders</button>
                
                <div id=""orderStatusResult"" class=""result"" style=""display:none; margin-top: 20px;""></div>
                <div id=""allOrdersResult"" class=""result"" style=""display:none; margin-top: 20px;""></div>
            </div>
        </div>

        <!-- Logs Tab -->
        <div id=""logs"" class=""tab-content"">
            <div class=""section"">
                <h3>📋 Recent Logs</h3>
                <button onclick='loadLogs()'>Refresh Logs</button>
                <div id='logsContainer'></div>
            </div>
        </div>

        <!-- Results Section -->
        <div id='result' class='result' style='display:none;'></div>
    </div>

    <script>
        // Tab management
        function showTab(tabName) {
            // Hide all tabs
            document.querySelectorAll('.tab-content').forEach(tab => {
                tab.classList.remove('active');
            });
            document.querySelectorAll('.tab-button').forEach(button => {
                button.classList.remove('active');
            });
            
            // Show selected tab
            document.getElementById(tabName).classList.add('active');
            event.target.classList.add('active');
            
            // Load data for specific tabs
            if (tabName === 'logs') loadLogs();
            if (tabName === 'status') loadAllOrders();
        }

        // Manual Callback Testing (JSON Body)
        async function sendManualCallback() {
            const paymobOrderId = document.getElementById('paymobOrderId').value;
            const transactionId = document.getElementById('transactionId').value;
            const amount = parseInt(document.getElementById('amount').value);
            const isSuccess = document.getElementById('success').value === 'true';

            const payload = {
                id: transactionId,
                pending: false,
                amount_cents: amount,
                success: isSuccess,
                is_auth: false,
                is_capture: false,
                is_standalone_payment: true,
                is_voided: false,
                is_refunded: false,
                is_3d_secure: true,
                integration_id: 5380556,
                profile_id: 1100113,
                has_parent_transaction: false,
                order: paymobOrderId,
                created_at: new Date().toISOString(),
                currency: 'EGP',
                error_occured: false,
                data: { message: isSuccess ? 'Approved' : 'Failed' }
            };

            await makeRequest('/User/Checkout/paymob/callback', 'POST', payload, 'Manual Callback (JSON Body)');
        }

        // Quick failed callback
        async function sendFailedCallback() {
            const paymobOrderId = document.getElementById('paymobOrderId').value;
            const transactionId = document.getElementById('transactionId').value;
            
            const payload = {
                id: transactionId,
                pending: false,
                amount_cents: 4400,
                success: false,
                order: paymobOrderId,
                currency: 'EGP',
                error_occured: true,
                data: { message: 'Payment failed' }
            };

            await makeRequest('/User/Checkout/paymob/callback', 'POST', payload, 'Failed Callback');
        }

        // Automatic Callback Simulation (Query Parameters)
        function generateAutoCallbackUrl() {
            const paymobOrderId = document.getElementById('autoPaymobOrderId').value;
            const amount = document.getElementById('autoAmount').value;
            
            const baseUrl = window.location.origin;
            const callbackUrl = `${baseUrl}/User/Checkout/paymob/callback?id=366427700&pending=false&amount_cents=${amount}&success=true&order=${paymobOrderId}&currency=EGP&data_message=Approved`;
            
            document.getElementById('generatedUrl').innerHTML = callbackUrl;
            
            // Copy to clipboard
            navigator.clipboard.writeText(callbackUrl).then(() => {
                alert('URL copied to clipboard!');
            });
        }

        async function testAutoCallback() {
            const paymobOrderId = document.getElementById('autoPaymobOrderId').value;
            const amount = document.getElementById('autoAmount').value;
            
            const url = `/User/Checkout/paymob/callback?id=366427700&pending=false&amount_cents=${amount}&success=true&order=${paymobOrderId}&currency=EGP&data_message=Approved`;
            
            await makeRequest(url, 'GET', null, 'Automatic Callback (Query Params)');
        }

        // Order Status Checking
        async function checkOrderStatus() {
            const paymobOrderId = document.getElementById('statusPaymobOrderId').value;
            
            try {
                const response = await fetch(`/User/Checkout/paymob/order-status/${paymobOrderId}`);
                const order = await response.json();
                
                const resultDiv = document.getElementById('orderStatusResult');
                resultDiv.style.display = 'block';
                resultDiv.className = 'result info';
                resultDiv.innerHTML = `
                    <h3>📊 Order Status</h3>
                    <pre>${JSON.stringify(order, null, 2)}</pre>
                `;
            } catch (error) {
                const resultDiv = document.getElementById('orderStatusResult');
                resultDiv.style.display = 'block';
                resultDiv.className = 'result error';
                resultDiv.innerHTML = `<h3>❌ Error</h3><p>${error.message}</p>`;
            }
        }

        async function loadAllOrders() {
            try {
                const response = await fetch('/User/Checkout/paymob/orders');
                const orders = await response.json();
                
                const resultDiv = document.getElementById('allOrdersResult');
                resultDiv.style.display = 'block';
                resultDiv.className = 'result info';
                resultDiv.innerHTML = `
                    <h3>📦 Recent Orders</h3>
                    <pre>${JSON.stringify(orders, null, 2)}</pre>
                `;
            } catch (error) {
                const resultDiv = document.getElementById('allOrdersResult');
                resultDiv.style.display = 'block';
                resultDiv.className = 'result error';
                resultDiv.innerHTML = `<h3>❌ Error</h3><p>${error.message}</p>`;
            }
        }

        // Generic request function
        async function makeRequest(url, method, payload, requestType) {
            try {
                const options = {
                    method: method,
                    headers: {
                        'Content-Type': 'application/json',
                    }
                };

                if (payload && method !== 'GET') {
                    options.body = JSON.stringify(payload);
                }

                const response = await fetch(url, options);
                const result = await response.json();
                
                displayResult(result, requestType, payload);
                loadLogs(); // Refresh logs after request
            } catch (error) {
                displayError(error, requestType);
            }
        }

        // Display results
        function displayResult(result, requestType, payload) {
            const resultDiv = document.getElementById('result');
            resultDiv.style.display = 'block';
            resultDiv.className = result.success ? 'result success' : 'result error';
            resultDiv.innerHTML = `
                <h3>${result.success ? '✅ Success' : '❌ Error'}</h3>
                <p><strong>Request Type:</strong> ${requestType}</p>
                <p><strong>Message:</strong> ${result.message || 'N/A'}</p>
                <p><strong>Order ID:</strong> ${result.orderId || 'N/A'}</p>
                <p><strong>Transaction:</strong> ${result.transactionReference || 'N/A'}</p>
                ${payload ? `<p><strong>Payload:</strong><br><pre style=""font-size: 10px;"">${JSON.stringify(payload, null, 2)}</pre></p>` : ''}
                <p><strong>Response:</strong><br><pre style=""font-size: 10px;"">${JSON.stringify(result, null, 2)}</pre></p>
            `;
        }

        function displayError(error, requestType) {
            const resultDiv = document.getElementById('result');
            resultDiv.style.display = 'block';
            resultDiv.className = 'result error';
            resultDiv.innerHTML = `
                <h3>❌ Request Failed</h3>
                <p><strong>Type:</strong> ${requestType}</p>
                <p><strong>Error:</strong> ${error.message}</p>
            `;
        }

        // Logs management
        async function loadLogs() {
            try {
                const response = await fetch('/User/Checkout/paymob/logs');
                const logs = await response.json();
                
                const container = document.getElementById('logsContainer');
                container.innerHTML = logs.map(log => `
                    <div class='log-entry log-${log.level.toLowerCase()}'>
                        <strong>${new Date(log.timestamp).toLocaleString()}</strong> 
                        [${log.level}] ${log.source}<br/>
                        <strong>Message:</strong> ${log.message}<br/>
                        ${log.details ? `<strong>Details:</strong> ${log.details}<br/>` : ''}
                        ${log.orderId ? `<strong>Order ID:</strong> ${log.orderId}<br/>` : ''}
                        ${log.paymobOrderId ? `<strong>Paymob Order:</strong> ${log.paymobOrderId}<br/>` : ''}
                        ${log.transactionId ? `<strong>Transaction:</strong> ${log.transactionId}<br/>` : ''}
                    </div>
                `).join('');
            } catch (error) {
                console.error('Error loading logs:', error);
            }
        }

        // Load logs on page load
        loadLogs();
    </script>
</body>
</html>";

            return Content(html, "text/html");
        }

        // NEW: Endpoint to get recent logs
        [HttpGet("paymob/logs")]
        public async Task<ActionResult> GetLogs()
        {
            var logs = await _context.Logs
                .OrderByDescending(l => l.Timestamp)
                .Take(50)
                .Select(l => new
                {
                    l.Timestamp,
                    l.Level,
                    l.Source,
                    l.Message,
                    l.Details,
                    l.OrderId,
                    l.PaymobOrderId,
                    l.TransactionId
                })
                .ToListAsync();

            return Ok(logs);
        }

        // NEW: Endpoint to check order status
        [HttpGet("paymob/order-status/{paymobOrderId}")]
        public async Task<ActionResult> GetOrderStatus(string paymobOrderId)
        {
            var order = await _context.Orders
                .Where(o => o.PaymobOrderId == paymobOrderId)
                .Select(o => new
                {
                    o.Id,
                    o.PaymobOrderId,
                    o.PaymentStatus,
                    o.Status,
                    o.TotalAmount,
                    o.TransactionId,
                    o.OrderDate
                })
                .FirstOrDefaultAsync();

            if (order == null)
            {
                return NotFound(new { message = "Order not found" });
            }

            return Ok(order);
        }

        // NEW: Endpoint to get all orders with Paymob IDs
        [HttpGet("paymob/orders")]
        public async Task<ActionResult> GetPaymobOrders()
        {
            var orders = await _context.Orders
                .Where(o => o.PaymobOrderId != null && o.PaymobOrderId != ".")
                .OrderByDescending(o => o.OrderDate)
                .Take(20)
                .Select(o => new
                {
                    o.Id,
                    o.PaymobOrderId,
                    o.PaymentStatus,
                    o.Status,
                    o.TotalAmount,
                    o.TransactionId,
                    o.OrderDate
                })
                .ToListAsync();

            return Ok(orders);
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