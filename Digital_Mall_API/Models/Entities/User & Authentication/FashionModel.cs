using Digital_Mall_API.Models.Entities.Financials;
using Digital_Mall_API.Models.Entities.Reels___Content;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Digital_Mall_API.Models.Entities.User___Authentication
{
    public class FashionModel
    {
        public Guid Id { get; set; }

        [Required]
        [StringLength(500)]
        public string Bio { get; set; }

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
        public virtual List<Reel>? Reels { get; set; } = new List<Reel>();
        public virtual List<Payout>? Payouts { get; set; } = new List<Payout>();
    }
}