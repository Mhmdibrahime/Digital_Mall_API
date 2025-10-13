using Digital_Mall_API.Models.Entities.Product_Catalog;
using Digital_Mall_API.Models.Entities.User___Authentication;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Digital_Mall_API.Models.Entities.Orders___Shopping
{
    public class OrderItem
    {
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        public int ProductVariantId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
        [Required]
        public string BrandId { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PriceAtTimeOfPurchase { get; set; }
        public bool IsRefunded { get; set; } = false;
        public int? RefundRequestId { get; set; } // Optional foreign key to RefundRequest

        public virtual Order? Order { get; set; }
        public virtual ProductVariant? ProductVariant { get; set; }
        public virtual Brand Brand { get; set; }
        // Optional one-to-one relationship with RefundRequest
        public virtual RefundRequest? RefundRequest { get; set; }
    }
}