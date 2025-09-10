namespace Digital_Mall_API.Models.DTOs.SuperAdminDTOs.OrdersManagementDTOs
{
    public class OrderDetailDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string BrandName { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentStatus { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentReference { get; set; }
        public string ShippingStatus { get; set; }
        public string ShippingTrackingNumber { get; set; }
        public DateTime EstimatedDelivery { get; set; }
        public string ShippingAddress { get; set; }
        public List<OrderItemDto> OrderItems { get; set; }
        public string Notes { get; set; }
    }
}
