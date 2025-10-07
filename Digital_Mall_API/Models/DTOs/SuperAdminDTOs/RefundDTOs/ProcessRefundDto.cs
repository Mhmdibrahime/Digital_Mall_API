using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.DTOs.SuperAdminDTOs.RefundDTOs
{
    public class ProcessRefundDto
    {
        [Required]
        public string Status { get; set; }

        [StringLength(500)]
        public string AdminNotes { get; set; }
    }
}
