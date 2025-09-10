using Digital_Mall_API.Models.Entities.Financials;
using Digital_Mall_API.Models.Entities.Reels___Content;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Digital_Mall_API.Models.Entities.User___Authentication
{
    public class FashionModel
    {
        public string Id { get; set; }

        [StringLength(500)]
        public string Name { get; set; }
        [Required]
        [StringLength(500)]
        public string Bio { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        

        [Range(0, 100)]
        [Column(TypeName = "decimal(5,2)")]
        public decimal? SpecificCommissionRate { get; set; }

        [Required]
        [StringLength(500)]
        public string EvidenceOfProofUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(100)]
        public string Password { get; set; }


        public virtual List<Reel>? Reels { get; set; } = new List<Reel>();
        public virtual List<Payout>? Payouts { get; set; } = new List<Payout>();
    }
}