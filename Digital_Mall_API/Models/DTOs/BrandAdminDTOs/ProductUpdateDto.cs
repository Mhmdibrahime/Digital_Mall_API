using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.DTOs.BrandAdminDTOs
{
    public class ProductUpdateDto
    {
        // Product fields
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
        public int SubSubCategoryId { get; set; }
        public string Gender { get; set; }

        // Product images
        public List<string> ImagesToDelete { get; set; } = new();
        public List<IFormFile> NewImages { get; set; } = new();

        // Variant data – parallel lists
        public List<string> VariantColors { get; set; } = new();
        public List<string>? VariantColorNames { get; set; } = new();      // NEW
        public List<string> VariantSizes { get; set; } = new();
        public List<int> VariantStockQuantities { get; set; } = new();
        public List<decimal?>? VariantPrices { get; set; } = new();        // NEW

        // Variant images
        public List<int> VariantImageIdsToDelete { get; set; } = new();
        public List<IFormFile> VariantImageFiles { get; set; } = new();
        public List<int> VariantImageIndices { get; set; } = new();
    }
}
