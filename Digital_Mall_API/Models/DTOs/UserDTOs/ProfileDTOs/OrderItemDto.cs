namespace Digital_Mall_API.Models.DTOs.UserDTOs.ProfileDTOs
{
    public class OrderItemDto
    {
        public int OrderItemId { get; set; }
        public string ProductName { get; set; }
        public string BrandName { get; set; }
        public string VariantInfo { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TotalPrice { get; set; }
        public string ImageUrl { get; set; }
        public bool HasRefundRequest { get; set; }
        public string RefundStatus { get; set; }
    }
}
