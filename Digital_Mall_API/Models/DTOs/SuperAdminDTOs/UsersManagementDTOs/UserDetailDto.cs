namespace Digital_Mall_API.Models.DTOs.SuperAdminDTOs.UsersManagementDTOs
{
    public class UserDetailDto
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string ProfilePictureUrl { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public int OrdersCount { get; set; }
        public decimal TotalSpent { get; set; }
        public int DesignOrdersCount { get; set; }
        public DateTime? LastOrderDate { get; set; }
        public decimal AverageOrderValue { get; set; }
    }

}
