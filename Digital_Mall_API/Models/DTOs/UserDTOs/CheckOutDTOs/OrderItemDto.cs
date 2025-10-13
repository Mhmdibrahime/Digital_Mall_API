using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.DTOs.UserDTOs.CheckOutDTOs
{
    public class OrderItemDto
    {
        [Required]
        public int ProductVariantId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }
}
