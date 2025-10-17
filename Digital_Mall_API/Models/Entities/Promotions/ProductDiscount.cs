using Digital_Mall_API.Models.Entities.Orders___Shopping;
using Digital_Mall_API.Models.Entities.Product_Catalog;
using Digital_Mall_API.Models.Entities.User___Authentication;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Digital_Mall_API.Models.Entities.Promotions
{
    public class ProductDiscount
    {
        public int Id { get; set; }

       
        [Required]
        [Range(0, 100)]
        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountValue { get; set; }


        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Active"; 

        public string BrandId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual List<Product> Products { get; set; } = new List<Product>();
        public virtual Brand Brand { get; set; }
    }

   
}