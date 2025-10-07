using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.Entities.User___Authentication
{
    public class FollowingBrand
    {
        public int Id { get; set; }

        [Required]
        public string CustomerId { get; set; }

        [Required]
        public string BrandId { get; set; }

        public DateTime FollowedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Customer Customer { get; set; }
        public virtual Brand Brand { get; set; }
    }
}