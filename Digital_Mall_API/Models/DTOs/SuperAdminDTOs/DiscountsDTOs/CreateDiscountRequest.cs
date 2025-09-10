using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.DTOs.SuperAdminDTOs.DiscountsDTOs
{
    public class CreateDiscountRequest
    {
        

        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        public string Status { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
