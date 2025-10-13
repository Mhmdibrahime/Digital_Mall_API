using Digital_Mall_API.Models.Entities.User___Authentication;

namespace Digital_Mall_API.Models.Entities.Orders___Shopping
{
    // Models/Entities/BrandStatistics.cs
    public class BrandStatistics
    {
        public int Id { get; set; }
        public string BrandId { get; set; }
        public int TotalRefunds { get; set; }
        public decimal TotalRefundAmount { get; set; }
        public DateTime LastUpdated { get; set; }

        public virtual Brand Brand { get; set; }
    }
}