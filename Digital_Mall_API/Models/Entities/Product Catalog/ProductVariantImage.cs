using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.Entities.Product_Catalog
{
    public class ProductVariantImage
    {
        public int Id { get; set; }

        [Required]
        public int ProductVariantId { get; set; }

        [Required]
        [StringLength(500)]
        public string ImageUrl { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int DisplayOrder { get; set; }

        public virtual ProductVariant? ProductVariant { get; set; }
    }
}
