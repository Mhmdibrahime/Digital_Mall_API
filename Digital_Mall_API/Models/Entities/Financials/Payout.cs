using Digital_Mall_API.Models.Entities.User___Authentication;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Digital_Mall_API.Models.Entities.Financials
{
    public class Payout
    {
        public int Id { get; set; }

        [Required]
        public Guid PayeeUserId { get; set; }

        [Required]
        [StringLength(50)]
        public string PayoutId { get; set; } 

        [Required]
        [Range(0.01, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime RequestDate { get; set; }

        public DateTime? ProcessedDate { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending"; 

        [Required]
        [StringLength(50)]
        public string Method { get; set; } 

        [Required]
        [StringLength(100)]
        public string BankAccountNumber { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public virtual ApplicationUser? PayeeUser { get; set; }
    }
}