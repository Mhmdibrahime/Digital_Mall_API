using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.DTOs.BrandAdminDTOs
{
    public class ProductUpdateDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
        public int SubCategoryId { get; set; }
        public string Gender { get; set; } = "Unisex";

        // Product‑level images
        public List<string> ImagesToDelete { get; set; } = new();      // URLs of images to delete
        public List<IFormFile> NewImages { get; set; } = new();        // New product images

        // Variant data – parallel lists (each index = one variant)
        public List<string> VariantColors { get; set; } = new();
        public List<string> VariantSizes { get; set; } = new();
        public List<int> VariantStockQuantities { get; set; } = new();

        // Variant images – flattened list with index mapping
        public List<IFormFile> VariantImageFiles { get; set; } = new();
        public List<int> VariantImageIndices { get; set; } = new();    // maps each file to a variant index

        // Variant images to delete (by ID)
        public List<int> VariantImageIdsToDelete { get; set; } = new();
    }
}
