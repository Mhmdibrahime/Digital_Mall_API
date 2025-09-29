namespace Digital_Mall_API.Models.DTOs.BrandAdminDTOs.ProductDiscountAndPromoCodeDTOs
{
    public class ProductDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string BrandName { get; set; } = string.Empty;
            public decimal OriginalPrice { get; set; }
            public decimal DiscountedPrice { get; set; }
        }
    
}
