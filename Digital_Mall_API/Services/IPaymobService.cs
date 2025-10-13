// Services/IPaymobService.cs
using Digital_Mall_API.Models.DTOs.UserDTOs.CheckOutDTOs;
using Digital_Mall_API.Models.Entities.Orders___Shopping;


namespace Digital_Mall_API.Services
{
    public interface IPaymobService
    {
        Task<PaymentInitResult> InitializePayment(Order order);
        Task<PaymentResultDto> HandleCallback(PaymobCallbackDto callbackData);
    }

    public class PaymentInitResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string PaymentUrl { get; set; }
        public string TransactionId { get; set; }
    }

    public class PaymobCallbackDto
    {
        public string TransactionId { get; set; }
        public string OrderId { get; set; }
        public bool Success { get; set; }
        public decimal Amount { get; set; }
        // Add other Paymob callback properties as needed
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