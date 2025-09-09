namespace Digital_Mall_API.Models.DTOs.SuperAdminDTOs.TShirtDesignersManagementDTOs
{
    public class DesignerDetailDto
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Location { get; set; }
        public string ProfilePictureUrl { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public int AssignedRequests { get; set; }
        public int CompletedRequests { get; set; }
        public decimal TotalEarnings { get; set; }
        public decimal PendingEarnings { get; set; }
        public double AverageRating { get; set; }
    }
}
