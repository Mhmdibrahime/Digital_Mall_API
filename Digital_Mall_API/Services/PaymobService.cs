using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.DTOs.UserDTOs.CheckOutDTOs;
using Digital_Mall_API.Models.Entities.Logs;
using Digital_Mall_API.Models.Entities.Orders___Shopping;
using Digital_Mall_API.Models.Entities.Logs;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Digital_Mall_API.Services
{
    
    public class PaymobService : IPaymobService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<PaymobService> _logger;
        private readonly AppDbContext _context;

        public PaymobService(IConfiguration configuration, HttpClient httpClient, ILogger<PaymobService> logger, AppDbContext context)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;
            _context = context;
        }

        public async Task LogToDatabase(string level, string source, string message, string? details = null,
            string? orderId = null, string? paymobOrderId = null, string? transactionId = null,
            string? additionalData = null)
        {
            try
            {
                var log = new Log
                {
                    Level = level,
                    Source = source,
                    Message = message,
                    Details = details,
                    OrderId = orderId,
                    PaymobOrderId = paymobOrderId,
                    TransactionId = transactionId,
                    AdditionalData = additionalData,
                    Timestamp = DateTime.UtcNow
                };

                _context.Logs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write log to database");
            }
        }

        public async Task<PaymentInitResult> InitializePayment(Order order, string customerId)
        {
            await LogToDatabase("Information", "PaymobService", "Initializing payment",
                $"Order: {order.Id}, Customer: {customerId}", order.Id.ToString());

            try
            {
                var apiKey = _configuration["Paymob:ApiKey"];
                var integrationId = _configuration["Paymob:IntegrationId"];
                var iframeId = _configuration["Paymob:IframeId"];

                // Validate configuration
                if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(integrationId) || string.IsNullOrEmpty(iframeId))
                {
                    await LogToDatabase("Error", "PaymobService", "Paymob configuration is missing",
                        null, order.Id.ToString());
                    return new PaymentInitResult { Success = false, Message = "Payment configuration error" };
                }

                // Step 1: Get authentication token
                var authResponse = await _httpClient.PostAsJsonAsync("https://accept.paymob.com/api/auth/tokens",
                    new { api_key = apiKey });

                if (!authResponse.IsSuccessStatusCode)
                {
                    var authError = await authResponse.Content.ReadAsStringAsync();
                    await LogToDatabase("Error", "PaymobService", "Paymob Auth Failed",
                        authError, order.Id.ToString());
                    return new PaymentInitResult { Success = false, Message = "Failed to authenticate with Paymob" };
                }

                var authResult = await authResponse.Content.ReadFromJsonAsync<PaymobAuthResponse>();
                var authToken = authResult.Token;

                await LogToDatabase("Information", "PaymobService", "Successfully obtained auth token",
                    null, order.Id.ToString());

                // Step 2: Create order
                var orderRequest = new
                {
                    auth_token = authToken,
                    delivery_needed = "false",
                    amount_cents = (int)(order.TotalAmount * 100),
                    currency = "EGP",
                    items = order.OrderItems.Select(oi => new
                    {
                        name = oi.ProductVariant?.Product.Name ?? $"Product {oi.ProductVariant.Product.Id}",
                        amount_cents = (int)(oi.PriceAtTimeOfPurchase * 100),
                        quantity = oi.Quantity,
                        description = oi.ProductVariant.Product?.Description ?? string.Empty
                    }).ToArray()
                };

                var orderResponse = await _httpClient.PostAsJsonAsync(
                    "https://accept.paymob.com/api/ecommerce/orders", orderRequest);

                if (!orderResponse.IsSuccessStatusCode)
                {
                    var orderError = await orderResponse.Content.ReadAsStringAsync();
                    await LogToDatabase("Error", "PaymobService", "Paymob Order Creation Failed",
                        orderError, order.Id.ToString());
                    return new PaymentInitResult { Success = false, Message = "Failed to create Paymob order" };
                }

                var orderResult = await orderResponse.Content.ReadFromJsonAsync<PaymobOrderResponse>();

                await LogToDatabase("Information", "PaymobService", "Successfully created Paymob order",
                    $"Paymob Order ID: {orderResult.Id}", order.Id.ToString(), orderResult.Id.ToString());

                // Save the Paymob order ID to database
                order.PaymobOrderId = orderResult.Id.ToString();
                await _context.SaveChangesAsync();

                await LogToDatabase("Information", "PaymobService", "Saved Paymob Order ID to database",
                    $"DB Order: {order.Id}, Paymob Order: {order.PaymobOrderId}", order.Id.ToString(), order.PaymobOrderId);

                var customer = await _context.Customers.FirstOrDefaultAsync(x => x.Id == customerId);
                if (customer == null)
                {
                    await LogToDatabase("Error", "PaymobService", "Customer not found",
                        null, order.Id.ToString());
                    return new PaymentInitResult { Success = false, Message = "Customer not found" };
                }

                // Step 3: Get payment key
                var paymentKeyRequest = new
                {
                    auth_token = authToken,
                    amount_cents = (int)(order.TotalAmount * 100),
                    expiration = 3600,
                    order_id = orderResult.Id.ToString(),
                    billing_data = new
                    {
                        apartment = "NA",
                        email = customer.Email?.Trim(),
                        floor = "NA",
                        first_name = GetFirstName(customer.FullName),
                        street = order.ShippingAddress_Street ?? "NA",
                        building = order.ShippingAddress_Building ?? "NA",
                        phone_number = FormatPhoneNumber(customer.PhoneNumber),
                        shipping_method = "NA",
                        postal_code = "NA",
                        city = order.ShippingAddress_City ?? "Cairo",
                        country = order.ShippingAddress_Country ?? "EG",
                        last_name = GetLastName(customer.FullName),
                        state = "NA"
                    },
                    currency = "EGP",
                    integration_id = int.Parse(integrationId)
                };

                var paymentKeyResponse = await _httpClient.PostAsJsonAsync(
                    "https://accept.paymob.com/api/acceptance/payment_keys", paymentKeyRequest);

                if (!paymentKeyResponse.IsSuccessStatusCode)
                {
                    var errorContent = await paymentKeyResponse.Content.ReadAsStringAsync();
                    await LogToDatabase("Error", "PaymobService", "Paymob Payment Key API Error",
                        errorContent, order.Id.ToString(), order.PaymobOrderId);
                    return new PaymentInitResult
                    {
                        Success = false,
                        Message = $"Failed to get payment key: {errorContent}"
                    };
                }

                var paymentKeyResult = await paymentKeyResponse.Content.ReadFromJsonAsync<PaymobPaymentKeyResponse>();

                await LogToDatabase("Information", "PaymobService", "Successfully obtained payment token",
                    null, order.Id.ToString(), order.PaymobOrderId, paymentKeyResult.Token);

                var paymentUrl = $"https://accept.paymob.com/api/acceptance/iframes/{iframeId}?payment_token={paymentKeyResult.Token}";

                return new PaymentInitResult
                {
                    Success = true,
                    Message = "Payment initialized successfully",
                    PaymentUrl = paymentUrl,
                    TransactionId = paymentKeyResult.Token,
                    PaymobOrderId = orderResult.Id.ToString()
                };
            }
            catch (Exception ex)
            {
                await LogToDatabase("Error", "PaymobService", "Error initializing Paymob payment",
                    ex.Message, order.Id.ToString());
                return new PaymentInitResult
                {
                    Success = false,
                    Message = "Error initializing payment"
                };
            }
        }

        public async Task<PaymentResultDto> HandleCallback(PaymobCallbackDto callbackData)
        {
            await LogToDatabase("Information", "PaymobService", "Callback received",
                $"Success: {callbackData.success}, Order: {callbackData.order}",
                null, callbackData.order, callbackData.id.ToString(),
                Newtonsoft.Json.JsonConvert.SerializeObject(callbackData));

            try
            {
                _logger.LogInformation("🔔 Paymob Callback Received - Success: {Success}, Paymob Order: {Order}, Message: {Message}",
                    callbackData.success, callbackData.order, callbackData.data?.message);

                // Find the actual database order using Paymob order ID
                var dbOrder = await _context.Orders
                    .FirstOrDefaultAsync(o => o.PaymobOrderId == callbackData.order);

                if (dbOrder == null)
                {
                    await LogToDatabase("Warning", "PaymobService", "No database order found for Paymob order",
                        $"Paymob Order: {callbackData.order}", null, callbackData.order);

                    return new PaymentResultDto
                    {
                        Success = false,
                        Message = "Order not found in database",
                        OrderId = 0,
                        TransactionReference = callbackData.id.ToString()
                    };
                }

                if (callbackData.success && !callbackData.error_occured)
                {
                    await LogToDatabase("Information", "PaymobService", "Payment successful",
                        $"DB Order: {dbOrder.Id}, Amount: {callbackData.amount_cents}",
                        dbOrder.Id.ToString(), callbackData.order, callbackData.id.ToString());

                    return new PaymentResultDto
                    {
                        Success = true,
                        Message = callbackData.data?.message ?? "Payment successful",
                        OrderId = dbOrder.Id,
                        TransactionReference = callbackData.id.ToString(),
                        Amount = callbackData.amount_cents / 100.0m,
                        Currency = callbackData.currency
                    };
                }
                else
                {
                    var errorMessage = callbackData.data?.message ?? "Payment failed";
                    await LogToDatabase("Warning", "PaymobService", "Payment failed",
                        errorMessage, dbOrder.Id.ToString(), callbackData.order, callbackData.id.ToString());

                    return new PaymentResultDto
                    {
                        Success = false,
                        Message = errorMessage,
                        OrderId = dbOrder.Id,
                        TransactionReference = callbackData.id.ToString(),
                        Amount = callbackData.amount_cents / 100.0m,
                        Currency = callbackData.currency
                    };
                }
            }
            catch (Exception ex)
            {
                await LogToDatabase("Error", "PaymobService", "Error handling Paymob callback",
                    ex.Message, null, callbackData.order, callbackData.id.ToString());

                return new PaymentResultDto
                {
                    Success = false,
                    Message = "Error processing payment callback"
                };
            }
        }

        private string GetFirstName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "Customer";
            var parts = fullName.Trim().Split(' ');
            return parts.FirstOrDefault() ?? "Customer";
        }

        private string GetLastName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "Name";
            var parts = fullName.Trim().Split(' ');
            return parts.Length > 1 ? parts.Last() : "Name";
        }

        private string FormatPhoneNumber(string phone)
        {
            if (string.IsNullOrEmpty(phone)) return "+201000000000";
            phone = phone.Trim().Replace(" ", "").Replace("-", "");
            if (phone.StartsWith("01") && phone.Length == 11) return "+2" + phone;
            if (phone.StartsWith("1") && phone.Length == 10) return "+20" + phone;
            if (!phone.StartsWith("+")) return "+20" + phone.TrimStart('0');
            return phone;
        }
    }
}