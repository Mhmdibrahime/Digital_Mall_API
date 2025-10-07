using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.DTOs.SuperAdminDTOs.RefundDTOs
{
    public class CreateRefundRequestDto
    {
        [Required]
        public int OrderId { get; set; }

        [Required]
        public int OrderItemId { get; set; }

        [Required]
        [StringLength(1000)]
        public string Reason { get; set; }

        public IFormFile ProductImage { get; set; }
    }
}
