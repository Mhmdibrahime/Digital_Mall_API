namespace Digital_Mall_API.Models.DTOs.SuperAdminDTOs.BrandsManagementDTOs
{
    public class BrandDto
    {
        public string Id { get; set; }
        public string OfficialName { get; set; }
        public string Description { get; set; }
        public string LogoUrl { get; set; }
        public string Status { get; set; }
        public decimal CommissionRate { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ProductsCount { get; set; }
        public int OrdersCount { get; set; }
    }
}
