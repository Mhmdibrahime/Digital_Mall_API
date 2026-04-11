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
        public int SubSubCategoryId { get; set; }
        public string Gender { get; set; } = "Unisex";

        // Product images
        public List<IFormFile>? Images { get; set; }

        // Variant data – parallel lists
        public List<string> VariantColors { get; set; } = new();
        public List<string>? VariantColorNames { get; set; } = new();   // اسم اللون (اختياري)
        public List<string> VariantSizes { get; set; } = new();
        public List<int> VariantStockQuantities { get; set; } = new();
        public List<decimal?>? VariantPrices { get; set; } = new();      // سعر خاص للمتغير (اختياري)

        // Variant images – flattened
        public List<IFormFile> VariantImageFiles { get; set; } = new();
        public List<int> VariantImageIndices { get; set; } = new();
    }
}
