namespace Digital_Mall_API.Models.DTOs.BrandAdminDTOs
{
    public class ProductQueryParameters
    {
        public string Search { get; set; }
        public int? CategoryId { get; set; }
        public int? SubCategoryId { get; set; }
        public bool? InStock { get; set; }
        public bool? IsActive { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

}
