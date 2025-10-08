namespace Digital_Mall_API.Models.DTOs.BrandAdminDTOs
{
    public class ProductCreateDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
        public int SubCategoryId { get; set; }
        public List<IFormFile> Images { get; set; }

    }

}
