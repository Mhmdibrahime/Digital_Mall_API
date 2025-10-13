using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.DTOs.UserDTOs.CheckOutDTOs
{
    public class ValidatePromoCodeCheckOutDto
    {
        [Required]
        public string PromoCode { get; set; }
        [Required]
        public List<OrderItemDto> OrderItems { get; set; }
    }
}
