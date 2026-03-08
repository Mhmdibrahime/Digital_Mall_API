namespace Digital_Mall_API.Models.DTOs.BrandAdminDTOs
{
    public class ProductCreateDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
        public int SubCategoryId { get; set; }
        public string Gender { get; set; } = "Unisex";
        public List<IFormFile>? Images { get; set; }
        public List<VariantCreateDto> Variants { get; set; } = new List<VariantCreateDto>();

    }
    public class ProductCreateFlatDto
    {
        // Product fields
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
        public int SubCategoryId { get; set; }
        public string Gender { get; set; } = "Unisex";

        // Product-level images
        public List<IFormFile>? Images { get; set; }

        // Variant data – parallel lists (each index = one variant)
        public List<string> VariantColors { get; set; } = new();
        public List<string> VariantSizes { get; set; } = new();
        public List<int> VariantStockQuantities { get; set; } = new();

        // Variant images – flattened list with index mapping
        public List<IFormFile> VariantImageFiles { get; set; } = new();
        public List<int> VariantImageIndices { get; set; } = new(); // maps each file to a variant index (0-based)
    }
}
