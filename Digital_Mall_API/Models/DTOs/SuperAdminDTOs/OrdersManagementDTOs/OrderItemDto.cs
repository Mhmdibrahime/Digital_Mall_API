namespace Digital_Mall_API.Models.DTOs.SuperAdminDTOs.OrdersManagementDTOs
{
    public class OrderItemDto
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total { get; set; }
    }
}
