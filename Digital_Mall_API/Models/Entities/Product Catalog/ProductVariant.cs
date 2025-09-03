using Digital_Mall_API.Models.Entities.Orders___Shopping;
using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.Entities.Product_Catalog
{
    public class ProductVariant
    {
        public Guid Id { get; set; }

        [Required]
        public Guid ProductId { get; set; }

        [Required]
        [StringLength(50)]
        public string Color { get; set; }

        [Required]
        [StringLength(20)]
        public string Size { get; set; }

        [Required]
        [StringLength(50)]
        public string Style { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }

        [Required]
        [StringLength(100)]
        public string? SKU { get; set; } // Stock Keeping Unit => TSH-BLK-M-SL

        public virtual Product? Product { get; set; }
        public virtual List<OrderItem>? OrderItems { get; set; } = new List<OrderItem>();
    }
}