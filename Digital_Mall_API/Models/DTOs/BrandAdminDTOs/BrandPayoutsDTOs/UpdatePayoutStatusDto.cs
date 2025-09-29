using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.DTOs.BrandAdminDTOs.BrandPayoutsDTOs
{
    public class UpdatePayoutStatusDto
    {
        [Required]
        [RegularExpression("^(Approved|Rejected|Completed)$")]
        public string Status { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Notes { get; set; }
    }
}
