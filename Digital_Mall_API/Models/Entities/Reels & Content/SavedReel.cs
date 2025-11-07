using Digital_Mall_API.Models.Entities.User___Authentication;

namespace Digital_Mall_API.Models.Entities.Reels___Content
{
    public class SavedReel
    {
        public int Id { get; set; }
        public int ReelId { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public DateTime SavedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Reel Reel { get; set; } = null!;
        public virtual Customer Customer { get; set; } = null!;
    }
}
