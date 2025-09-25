namespace Digital_Mall_API.Models.DTOs.BrandAdminDTOs
{
    public class ProductVariantCreateUpdateDto
    {
        public string Color { get; set; }
        public string Size { get; set; }
        public string Style { get; set; }
        public int StockQuantity { get; set; }
        public string SKU { get; set; }
    }

}
