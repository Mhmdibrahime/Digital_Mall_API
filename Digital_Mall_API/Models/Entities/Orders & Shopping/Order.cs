using Digital_Mall_API.Models.Entities.User___Authentication;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Digital_Mall_API.Models.Entities.Orders___Shopping
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        public string CustomerId { get; set; }

        [Required]
        public string BrandId { get; set; }

        [Required]
        public DateTime OrderDate { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [StringLength(100)]
        public string ShippingAddress_Building { get; set; }

        [Required]
        [StringLength(200)]
        public string ShippingAddress_Street { get; set; }

        [Required]
        [StringLength(100)]
        public string ShippingAddress_City { get; set; }

        [Required]
        [StringLength(100)]
        public string ShippingAddress_Country { get; set; }

        [Required]
        [StringLength(20)]
        public string PaymentMethod_Type { get; set; }

        [StringLength(50)]
        public string ShippingTrackingNumber { get; set; }

        [Required]
        public bool IsPaid { get; set; }

        public virtual Customer? Customer{ get; set; }
        public virtual Brand? Brand { get; set; }
        public virtual List<OrderItem>? OrderItems { get; set; } = new List<OrderItem>();
    }
}