using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.Entities.Product_Catalog
{
    public class SubCategory
    {
        public int Id { get; set; }
        [StringLength(100)]
        public string Name { get; set; }

        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        [StringLength(255)]
        public string ImageUrl { get; set; }

        public ICollection<Product>? Products { get; set; }
    }
}