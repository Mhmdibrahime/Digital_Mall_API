using Digital_Mall_API.Models.Entities.Financials;
using Digital_Mall_API.Models.Entities.Orders___Shopping;
using Digital_Mall_API.Models.Entities.Product_Catalog;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Digital_Mall_API.Models.Entities.User___Authentication
{
    public class Brand
    {
        public Guid Id { get; set; }

        [Required]
        [StringLength(200)]
        public string OfficialName { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        [Required]
        [Url]
        [StringLength(500)]
        public string LogoUrl { get; set; }

        [StringLength(100)]
        public string? CommercialRegistrationNumber { get; set; }

        [StringLength(100)]
        public string? TaxCardNumber { get; set; }

        [Required]
        public bool IsApproved { get; set; } = false;

        [Required]
        [Range(0, 100)]
        [Column(TypeName = "decimal(5,2)")]
        public decimal CommissionRate { get; set; }

        [Required]
        [StringLength(500)]
        public string EvidenceOfProofUrl { get; set; } 

        public virtual ApplicationUser? User { get; set; }
        public virtual List<Product>? Products { get; set; } = new List<Product>();
        public virtual List<Order>? Orders { get; set; } = new List<Order>();
        public virtual List<Payout>? Payouts { get; set; } = new List<Payout>();
    }
}