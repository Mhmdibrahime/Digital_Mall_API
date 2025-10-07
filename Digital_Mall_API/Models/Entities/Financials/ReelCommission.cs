using Digital_Mall_API.Models.Entities.Orders___Shopping;
using Digital_Mall_API.Models.Entities.Product_Catalog;
using Digital_Mall_API.Models.Entities.Reels___Content;
using Digital_Mall_API.Models.Entities.User___Authentication;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Digital_Mall_API.Models.Entities.Financials
{
    public class ReelCommission
    {
        public int Id { get; set; }

        [Required]
        public string FashionModelId { get; set; }

        [Required]
        public string BrandId { get; set; } 

        [Required]
        public int OrderId { get; set; }

        [Required]
        public int OrderItemId { get; set; }

        [Required]
        public int ReelId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal SaleAmount { get; set; }

        [Required]
        [Range(0, 100)]
        [Column(TypeName = "decimal(5,2)")]
        public decimal CommissionRate { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CommissionAmount { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; }

        public virtual FashionModel FashionModel { get; set; }
        public virtual Brand Brand { get; set; } 
        public virtual Order Order { get; set; }
        public virtual OrderItem OrderItem { get; set; }
        public virtual Reel Reel { get; set; }
        public virtual Product Product { get; set; }
    }
}
