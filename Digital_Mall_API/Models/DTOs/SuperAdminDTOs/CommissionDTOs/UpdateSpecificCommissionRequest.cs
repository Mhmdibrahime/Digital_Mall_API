using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.DTOs.SuperAdminDTOs.CommissionDTOs
{
    public class UpdateSpecificCommissionRequest
    {
        [Required]
        [Range(0, 100)]
        public decimal CommissionRate { get; set; }
    }
}
