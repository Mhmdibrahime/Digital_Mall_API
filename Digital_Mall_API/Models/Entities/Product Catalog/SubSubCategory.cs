using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.Entities.Product_Catalog
{
    public class SubSubCategory
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string EnglishName { get; set; }

        [StringLength(100)]
        public string? ArabicName { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(255)]
        public string? ImageUrl { get; set; }

        public int SubCategoryId { get; set; }
        public virtual SubCategory? SubCategory { get; set; }

        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}