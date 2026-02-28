using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.DTOs.BrandAdminDTOs
{
    public class ProductUpdateDto
    {
        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        public bool IsActive { get; set; }

        public string Gender { get; set; }

        public int SubCategoryId { get; set; }

        // New images to upload
        public List<IFormFile>? NewImages { get; set; }

        // URLs of existing images to delete
        public List<string>? ImagesToDelete { get; set; }

       
    }
}
