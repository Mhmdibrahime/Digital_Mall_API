namespace Digital_Mall_API.Models.DTOs.UserDTOs.CheckOutDTOs
{
    public class OrderItemDiscountDto
    {
        public int ProductVariantId { get; set; }
        public int Quantity { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal DiscountedPrice { get; set; }
        public decimal DiscountAmount { get; set; }
    }
}
