namespace Digital_Mall_API.Models.DTOs.UserDTOs.CheckOutDTOs
{
    public class PaymentResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int OrderId { get; set; }
        public string TransactionReference { get; set; }
    }
}
