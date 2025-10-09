namespace Digital_Mall_API.Models.DTOs.SuperAdminDTOs.ModelsManagementDTOs
{
    public class ModelDto
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Bio { get; set; }
        public string Status { get; set; }
        public string Email { get; set; }
        
        public decimal CommissionRate { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ReelsCount { get; set; }
        public int TotalLikes { get; set; }
        public decimal TotalEarnings { get; set; }
    }
}
