using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.Entities.User___Authentication
{
    public class FollowingModel
    {
        public int Id { get; set; }

        [Required]
        public string CustomerId { get; set; }

        [Required]
        public string FashionModelId { get; set; }

        public DateTime FollowedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Customer Customer { get; set; }
        public virtual FashionModel FashionModel { get; set; }
    }
}