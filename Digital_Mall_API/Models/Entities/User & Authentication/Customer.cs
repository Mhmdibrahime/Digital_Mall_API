using Digital_Mall_API.Models.Entities.Orders___Shopping;
using Digital_Mall_API.Models.Entities.T_Shirt_Customization;

namespace Digital_Mall_API.Models.Entities.User___Authentication
{
    public class Customer
    {
        public Guid Id { get; set; }
        public virtual ApplicationUser? User { get; set; }
        public virtual List<Order>? Orders { get; set; } = new List<Order>();
        public virtual List<TshirtDesignOrder>? DesignOrders { get; set; } = new List<TshirtDesignOrder>();
    }
}