namespace Digital_Mall_API.Models.DTOs.SuperAdminDTOs.CategoriesManagementDTOs
{
    public class CategoryResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public List<SubCategoryResponseDto> SubCategories { get; set; } = new List<SubCategoryResponseDto>();
        public int TotalProducts { get; set; }
    }
}
