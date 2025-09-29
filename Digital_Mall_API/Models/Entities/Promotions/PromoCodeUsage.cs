using Digital_Mall_API.Models.Entities.Orders___Shopping;
using Digital_Mall_API.Models.Entities.User___Authentication;
using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.Entities.Promotions
{
    public class PromoCodeUsage
    {
        public int Id { get; set; }

        [Required]
        public int PromoCodeId { get; set; }

        [Required]
        public string CustomerId { get; set; }

        [Required]
        public int OrderId { get; set; }

        public DateTime UsedAt { get; set; } = DateTime.UtcNow;

        public decimal DiscountAmount { get; set; }
        public decimal OrderTotal { get; set; }

        public virtual PromoCode PromoCode { get; set; }
        public virtual Customer Customer { get; set; }
        public virtual Order Order { get; set; }
    }
}