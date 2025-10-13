using Digital_Mall_API.Models.Entities.User___Authentication;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Digital_Mall_API.Models.Entities.Orders___Shopping
{
    public class RefundRequest
    {
        public int Id { get; set; }

        [Required]
        public string RefundNumber { get; set; } = $"REF-{DateTime.Now:yyyyMMddHHmmss}";

        [Required]
        public int OrderId { get; set; }

        [Required]
        public int OrderItemId { get; set; }

        [Required]
        public string CustomerId { get; set; }

        [Required]
        [StringLength(1000)]
        public string Reason { get; set; }

        [StringLength(500)]
        public string ImageUrl { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending"; 

        public DateTime RequestDate { get; set; } = DateTime.UtcNow;

        public DateTime? ProcessedDate { get; set; }

        [StringLength(500)]
        public string? AdminNotes { get; set; }
        public decimal RefundAmount { get; set; } = 0.0m;

        public virtual Order Order { get; set; }
        public virtual OrderItem OrderItem { get; set; }
        public virtual Customer Customer { get; set; }
    }
}