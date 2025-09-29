using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.DTOs.BrandAdminDTOs.BrandPayoutsDTOs
{
    public class CreatePayoutDto
    {
        [Required]
        public Guid PayeeUserId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(50)]
        public string Method { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string BankAccountNumber { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Notes { get; set; }
    }
}
