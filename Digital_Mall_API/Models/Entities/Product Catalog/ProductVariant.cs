using Digital_Mall_API.Models.Entities.Orders___Shopping;
using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.Entities.Product_Catalog
{
    public class ProductVariant
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        [StringLength(50)]
        public string Color { get; set; }

        [Required]
        [StringLength(20)]
        public string Size { get; set; }

       

        [Required]
        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }

        

        public virtual Product? Product { get; set; }
        public virtual List<OrderItem>? OrderItems { get; set; } = new List<OrderItem>();
    }
}