using Digital_Mall_API.Models.Entities.Product_Catalog;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Digital_Mall_API.Models.Entities.Orders___Shopping
{
    public class OrderItem
    {
        public Guid Id { get; set; }

        [Required]
        public Guid OrderId { get; set; }

        [Required]
        public Guid ProductVariantId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PriceAtTimeOfPurchase { get; set; }

        public virtual Order? Order { get; set; }
        public virtual ProductVariant? ProductVariant { get; set; }
    }
}