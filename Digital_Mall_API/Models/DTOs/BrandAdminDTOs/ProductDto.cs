namespace Digital_Mall_API.Models.DTOs.BrandAdminDTOs
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public string CategoryName { get; set; }
        public string SubCategoryName { get; set; }
  
        public List<ProductVariantDto> Variants { get; set; } = new();
        public List<string> Images { get; set; } = new();
    }

}
