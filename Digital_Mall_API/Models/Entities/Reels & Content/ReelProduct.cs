using Digital_Mall_API.Models.Entities.Product_Catalog;
using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.Entities.Reels___Content
{
    public class ReelProduct
    {
        [Required]
        public Guid ReelId { get; set; }

        [Required]
        public Guid ProductId { get; set; }

        public virtual Reel? Reel { get; set; }
        public virtual Product? Product { get; set; }
    }
}