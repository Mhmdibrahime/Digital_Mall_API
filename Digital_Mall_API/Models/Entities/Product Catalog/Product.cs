using Digital_Mall_API.Models.Entities.Reels___Content;
using Digital_Mall_API.Models.Entities.User___Authentication;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Digital_Mall_API.Models.Entities.Product_Catalog
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        public string BrandId { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public virtual Brand? Brand { get; set; }
        public virtual Category? Category { get; set; }
        public virtual List<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
        public virtual List<ProductImage> Images { get; set; } = new List<ProductImage>();
        public virtual List<ReelProduct> ReelProducts { get; set; } = new List<ReelProduct>();
    }
}