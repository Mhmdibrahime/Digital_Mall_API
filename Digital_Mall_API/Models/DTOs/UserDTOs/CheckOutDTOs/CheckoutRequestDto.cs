using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.DTOs.UserDTOs.CheckOutDTOs
{
    public class CheckoutRequestDto
    {
        [Required]
        public List<OrderItemDto> OrderItems { get; set; } = new List<OrderItemDto>();

        [Required]
        public string ShippingAddress_Building { get; set; }

        [Required]
        public string ShippingAddress_Street { get; set; }

        [Required]
        public string ShippingAddress_City { get; set; }

        [Required]
        public string ShippingAddress_Country { get; set; }
        public string? ShippingTrackingNumber { get; set; }

        [Required]
        public string PaymentMethod { get; set; } // "wallet" or "paymob"

        public string Notes { get; set; }
        
        public string? PromoCode { get; set; }
    }
}
