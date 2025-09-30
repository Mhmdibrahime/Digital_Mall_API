using Digital_Mall_API.Models.Entities.Financials;
using Digital_Mall_API.Models.Entities.Orders___Shopping;
using Digital_Mall_API.Models.Entities.Product_Catalog;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Digital_Mall_API.Models.Entities.User___Authentication
{
    public class Brand
    {
        public string Id { get; set; }

        [Required]
        [StringLength(200)]
        public string OfficialName { get; set; }

        [StringLength(200)]
        public string? Facebook { get; set; }
        [StringLength(200)]
        public string? Instgram { get; set; }

        public bool Online { get; set; }
        public bool Ofline { get; set; }
      
        [StringLength(1000)]

        public string? Description { get; set; }

      
        [Url]
        [StringLength(500)]
        public string? LogoUrl { get; set; }

        [StringLength(100)]
        public string? CommercialRegistrationNumber { get; set; }

        [StringLength(100)]
        public string? TaxCardNumber { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(100)]
        public string Password { get; set; }

        [StringLength(5000)]
        public  string? ReturnPolicy { get; set; }

        [Range(0, 100)]
        [Column(TypeName = "decimal(5,2)")]
        public decimal? SpecificCommissionRate { get; set; }

        [Required]
        [StringLength(500)]
        public string EvidenceOfProofUrl { get; set; } 

        public virtual List<Product>? Products { get; set; } = new List<Product>();
        public virtual List<Order>? Orders { get; set; } = new List<Order>();
        public virtual List<Payout>? Payouts { get; set; } = new List<Payout>();
    }
}