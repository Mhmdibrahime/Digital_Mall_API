namespace Digital_Mall_API.Models.DTOs.BrandAdminDTOs
{
    public class ProductVariantDto
    {
        public int Id { get; set; }
        public string Color { get; set; }
        public string? ColorName { get; set; }       // اسم اللون (مثل "أحمر")
        public string Size { get; set; }
        public int StockQuantity { get; set; }
        public decimal? Price { get; set; }          // سعر خاص للمتغير (إذا كان null، يستخدم سعر المنتج)
        public List<string> Images { get; set; } = new();
    }

}
