using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.DTOs.SuperAdminDTOs.DiscountsDTOs
{
    public class CreateDiscountRequest
    {
        

        public IFormFile File { get; set; }

        [Required]
        public string Status { get; set; } = "Active";


    }
}
