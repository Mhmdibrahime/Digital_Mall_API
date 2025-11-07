// Services/IPaymobService.cs
using Digital_Mall_API.Models.DTOs.UserDTOs.CheckOutDTOs;
using Digital_Mall_API.Models.Entities.Orders___Shopping;


namespace Digital_Mall_API.Services
{
    public interface IPaymobService
    {
        Task<PaymentInitResult> InitializePayment(Order order, string customerId);
        Task<PaymentResultDto> HandleCallback(PaymobCallbackDto callbackData);
        Task LogToDatabase(string level, string source, string message, string? details = null,
            string? orderId = null, string? paymobOrderId = null, string? transactionId = null,
            string? additionalData = null);
    }

    public class PaymentInitResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string PaymentUrl { get; set; }
        public string TransactionId { get; set; }
        public string PaymobOrderId { get; set; } // Add this
    }

    // In your DTOs
    public class PaymobCallbackDto
    {
        public long id { get; set; }
        public bool pending { get; set; }
        public long amount_cents { get; set; }
        public bool success { get; set; }
        public bool is_auth { get; set; }
        public bool is_capture { get; set; }
        public bool is_standalone_payment { get; set; }
        public bool is_voided { get; set; }
        public bool is_refunded { get; set; }
        public bool is_3d_secure { get; set; }
        public int integration_id { get; set; }
        public int profile_id { get; set; }
        public bool has_parent_transaction { get; set; }
        public string order { get; set; } = string.Empty;
        public DateTime created_at { get; set; }
        public string currency { get; set; } = "EGP";
        public bool error_occured { get; set; }
        public PaymobCallbackData data { get; set; } = new PaymobCallbackData();
    }

    public class PaymobCallbackData
    {
        public string message { get; set; } = string.Empty;
    }

    public class PaymobSourceData
    {
        public string type { get; set; }
        public string pan { get; set; }
        public string sub_type { get; set; }
    }
}




namespace Digital_Mall_API.Services
{

    // Paymob response classes
    public class PaymobAuthResponse
    {
        public string Token { get; set; }
    }

    public class PaymobOrderResponse
    {
        public int Id { get; set; }
    }

    public class PaymobPaymentKeyResponse
    {
        public string Token { get; set; }
    }
}