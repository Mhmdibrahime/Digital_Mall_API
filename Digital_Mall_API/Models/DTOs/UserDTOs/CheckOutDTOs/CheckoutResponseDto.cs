namespace Digital_Mall_API.Models.DTOs.UserDTOs.CheckOutDTOs
{
    public class CheckoutResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int? OrderId { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentUrl { get; set; } // For Paymob redirect
        public string TransactionId { get; set; } // For Paymob
    }
}
