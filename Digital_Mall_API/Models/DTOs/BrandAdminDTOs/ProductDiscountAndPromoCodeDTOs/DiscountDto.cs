namespace Digital_Mall_API.Models.DTOs.BrandAdminDTOs.ProductDiscountAndPromoCodeDTOs
{
        public class DiscountDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public decimal DiscountValue { get; set; }
            public string DiscountType { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string? PromoCode { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
            public List<ProductDto> Products { get; set; } = new List<ProductDto>();
        }
    
}
