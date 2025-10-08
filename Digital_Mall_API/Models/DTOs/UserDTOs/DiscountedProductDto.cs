namespace Digital_Mall_API.Models.DTOs.UserDTOs
{
    public class DiscountedProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public string? BrandName { get; set; }
        public string? ImageUrl { get; set; }

        public decimal OriginalPrice { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal DiscountedPrice { get; set; }

        public string DiscountStatus { get; set; }
        public DateTime CreatedAt { get; set; }

        public int StockQuantity { get; set; } 
    }
}
