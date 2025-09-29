namespace Digital_Mall_API.Models.DTOs.BrandAdminDTOs
{
    public class ProductCreateUpdateDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
        public int SubCategoryId { get; set; }
        public List<ProductVariantCreateUpdateDto> Variants { get; set; } = new();
    }

}
