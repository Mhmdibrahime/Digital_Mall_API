namespace Digital_Mall_API.Models.DTOs.SuperAdminDTOs.UsersManagementDTOs
{
    public class UserDto
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public int OrdersCount { get; set; }
        public decimal TotalSpent { get; set; }
        public int DesignOrdersCount { get; set; }
    }

}
