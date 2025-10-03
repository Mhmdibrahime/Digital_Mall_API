namespace Digital_Mall_API.Models.DTOs.SuperAdminDTOs.TrendingDTOs
{
    public class ProductResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string ProductId { get; set; }
        public string Brand { get; set; }
        public string Status { get; set; }
        public decimal Price { get; set; }
        public bool IsTrend { get; set; }
        public List<ProductImageDto> Images { get; set; } = new List<ProductImageDto>();
    }
}
