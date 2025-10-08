using Digital_Mall_API.Models.Entities.User___Authentication;
using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.Entities.Reels___Content
{
    public class ReelLike
    {
        public int Id { get; set; }

        [Required]
        public int ReelId { get; set; }

        [Required]
        public string CustomerId { get; set; }

        public DateTime LikedAt { get; set; } = DateTime.UtcNow;

        public virtual Reel Reel { get; set; }
        public virtual Customer Customer { get; set; }
    }
}