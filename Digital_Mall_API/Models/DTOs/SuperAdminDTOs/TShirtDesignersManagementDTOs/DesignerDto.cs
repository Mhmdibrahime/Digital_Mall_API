namespace Digital_Mall_API.Models.DTOs.SuperAdminDTOs.TShirtDesignersManagementDTOs
{
    public class DesignerDto
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public int AssignedRequests { get; set; }
        public decimal TotalEarnings { get; set; }
    }
}
