namespace Digital_Mall_API.Models.DTOs.SuperAdminDTOs.CategoriesManagementDTOs
{
    public class SubCategoryResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int ProductCount { get; set; }
    }
}
