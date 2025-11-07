using Digital_Mall_API.Models.Entities.User___Authentication;

namespace Digital_Mall_API.Models.Entities.Reels___Content
{
    public class ReelReport
    {
        public int Id { get; set; }
        public int ReelId { get; set; }
        public string ReportedByCustomerId { get; set; }
        public string Reason { get; set; }
        public string AdditionalDetails { get; set; }
        public DateTime ReportedAt { get; set; }
        public string Status { get; set; } // Pending, Reviewed, Resolved, etc.

        // Navigation properties
        public Reel Reel { get; set; }
        public Customer ReportedByCustomer { get; set; }
    }
}
