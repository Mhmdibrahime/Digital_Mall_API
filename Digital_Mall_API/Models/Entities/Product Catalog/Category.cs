using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.Entities.Product_Catalog
{
    public class Category
    {
        public Guid Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [StringLength(500)]
        public string ImageUrl { get; set; }

        public virtual List<Product>? Products { get; set; } = new List<Product>();
    }
}