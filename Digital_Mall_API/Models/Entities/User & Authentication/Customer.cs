using Digital_Mall_API.Models.Entities.Orders___Shopping;
using Digital_Mall_API.Models.Entities.T_Shirt_Customization;
using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.Entities.User___Authentication
{
    public class Customer
    {
        public string Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(100)]
        public string Password { get; set; }
        [StringLength(50)]
        public string Status { get; set; } = "Active";
        public virtual List<Order>? Orders { get; set; } = new List<Order>();
        public virtual List<TshirtDesignOrder>? DesignOrders { get; set; } = new List<TshirtDesignOrder>();
    }
}