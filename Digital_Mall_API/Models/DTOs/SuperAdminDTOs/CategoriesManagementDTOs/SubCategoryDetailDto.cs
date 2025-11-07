namespace Digital_Mall_API.Models.DTOs.SuperAdminDTOs.CategoriesManagementDTOs
{
    public class SubCategoryDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool HasImage { get; set; }
    }
}
