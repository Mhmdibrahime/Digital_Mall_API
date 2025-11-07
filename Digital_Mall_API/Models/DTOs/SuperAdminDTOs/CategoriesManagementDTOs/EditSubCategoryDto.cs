using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.DTOs.SuperAdminDTOs.CategoriesManagementDTOs
{
    public class EditSubCategoryDto
    {
        [Required(ErrorMessage = "Subcategory name is required")]
        [StringLength(100, ErrorMessage = "Subcategory name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category ID is required")]
        public int CategoryId { get; set; }

        public IFormFile? Image { get; set; }
        public bool RemoveImage { get; set; }
    }
}
