using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.DTOs.SuperAdminDTOs.CategoriesManagementDTOs
{
    public class SubCategoryResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? ArabicName { get; set; }

        public string ImageUrl { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int ProductCount { get; set; }
    }
    public class SubSubCategoryResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; }         
        public string? ArabicName { get; set; }
    
        public string? ImageUrl { get; set; }
        public int SubCategoryId { get; set; }
        public string? SubCategoryName { get; set; }
        public int ProductCount { get; set; }     


    }

    public class CreateSubSubCategoryPopupDto
    {
        [Required]
        public string Name { get; set; }         
        public string? ArabicName { get; set; }
     
        public IFormFile? Image { get; set; }

        [Required]
        public int SubCategoryId { get; set; }      
    }

    public class UpdateSubSubCategoryDto
    {
        public string? Name { get; set; }
        public string? ArabicName { get; set; }
        public string? Description { get; set; }
        public IFormFile? Image { get; set; }
        public bool RemoveImage { get; set; }       
    }
}
