namespace Digital_Mall_API.Models.DTOs.UserDTOs.ProfileDTOs
{
    public class UserOrderDto
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; }
        public string PaymentStatus { get; set; }
        public decimal TotalAmount { get; set; }
        public int ItemCount { get; set; }
        public string ShippingAddress { get; set; }
        public List<OrderItemDto> OrderItems { get; set; } = new List<OrderItemDto>();
        public bool HasRefundRequest { get; set; }
        public string RefundStatus { get; set; }
    }
}
