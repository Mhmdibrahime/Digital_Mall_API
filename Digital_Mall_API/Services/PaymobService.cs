using Digital_Mall_API.Models.DTOs.UserDTOs.CheckOutDTOs;
using Digital_Mall_API.Models.Entities.Orders___Shopping;

namespace Digital_Mall_API.Services
{
    public class PaymobService : IPaymobService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<PaymobService> _logger;

        public PaymobService(IConfiguration configuration, HttpClient httpClient, ILogger<PaymobService> logger)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<PaymentInitResult> InitializePayment(Order order)
        {
            try
            {
                // Implement Paymob payment initialization
                // This is a simplified version - you'll need to implement according to Paymob's API

                var apiKey = _configuration["Paymob:ApiKey"];
                var integrationId = _configuration["Paymob:IntegrationId"];
                var iframeId = _configuration["Paymob:IframeId"];

                // Step 1: Get authentication token
                var authResponse = await _httpClient.PostAsJsonAsync("https://accept.paymob.com/api/auth/tokens",
                    new { api_key = apiKey });

                if (!authResponse.IsSuccessStatusCode)
                {
                    return new PaymentInitResult
                    {
                        Success = false,
                        Message = "Failed to authenticate with Paymob"
                    };
                }

                var authResult = await authResponse.Content.ReadFromJsonAsync<PaymobAuthResponse>();
                var authToken = authResult.Token;

                // Step 2: Create order
                var orderRequest = new
                {
                    auth_token = authToken,
                    delivery_needed = "false",
                    amount_cents = (int)(order.TotalAmount * 100), // Convert to cents
                    currency = "EGP",
                    items = order.OrderItems.Select(oi => new
                    {
                        name = $"Order Item {oi.Id}",
                        amount_cents = (int)(oi.PriceAtTimeOfPurchase * 100),
                        quantity = oi.Quantity
                    }).ToArray()
                };

                var orderResponse = await _httpClient.PostAsJsonAsync(
                    "https://accept.paymob.com/api/ecommerce/orders", orderRequest);

                if (!orderResponse.IsSuccessStatusCode)
                {
                    return new PaymentInitResult
                    {
                        Success = false,
                        Message = "Failed to create Paymob order"
                    };
                }

                var orderResult = await orderResponse.Content.ReadFromJsonAsync<PaymobOrderResponse>();

                // Step 3: Get payment key
                var paymentKeyRequest = new
                {
                    auth_token = authToken,
                    amount_cents = (int)(order.TotalAmount * 100),
                    expiration = 3600,
                    order_id = orderResult.Id,
                    billing_data = new
                    {
                        apartment = "NA",
                        email = "customer@example.com",
                        floor = "NA",
                        first_name = "Customer",
                        street = order.ShippingAddress_Street,
                        building = order.ShippingAddress_Building,
                        phone_number = "+201000000000", // You might want to store customer phone
                        shipping_method = "NA",
                        postal_code = "NA",
                        city = order.ShippingAddress_City,
                        country = order.ShippingAddress_Country,
                        last_name = "Customer",
                        state = "NA"
                    },
                    currency = "EGP",
                    integration_id = integrationId
                };

                var paymentKeyResponse = await _httpClient.PostAsJsonAsync(
                    "https://accept.paymob.com/api/acceptance/payment_keys", paymentKeyRequest);

                if (!paymentKeyResponse.IsSuccessStatusCode)
                {
                    return new PaymentInitResult
                    {
                        Success = false,
                        Message = "Failed to get payment key"
                    };
                }

                var paymentKeyResult = await paymentKeyResponse.Content.ReadFromJsonAsync<PaymobPaymentKeyResponse>();

                // Return payment URL
                var paymentUrl = $"https://accept.paymob.com/api/acceptance/iframes/{iframeId}?payment_token={paymentKeyResult.Token}";

                return new PaymentInitResult
                {
                    Success = true,
                    Message = "Payment initialized successfully",
                    PaymentUrl = paymentUrl,
                    TransactionId = paymentKeyResult.Token
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing Paymob payment");
                return new PaymentInitResult
                {
                    Success = false,
                    Message = "Error initializing payment"
                };
            }
        }

        public async Task<PaymentResultDto> HandleCallback(PaymobCallbackDto callbackData)
        {
            try
            {
                // Validate the callback with Paymob (you should verify the HMAC signature)
                if (callbackData.Success)
                {
                    return new PaymentResultDto
                    {
                        Success = true,
                        Message = "Payment successful",
                        OrderId = int.Parse(callbackData.OrderId),
                        TransactionReference = callbackData.TransactionId
                    };
                }
                else
                {
                    return new PaymentResultDto
                    {
                        Success = false,
                        Message = "Payment failed",
                        OrderId = int.Parse(callbackData.OrderId),
                        TransactionReference = callbackData.TransactionId
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling Paymob callback");
                return new PaymentResultDto
                {
                    Success = false,
                    Message = "Error processing payment callback"
                };
            }
        }
    }
}