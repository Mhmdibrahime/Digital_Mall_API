namespace Digital_Mall_API.Models.DTOs.UserDTOs.ProfileDTOs
{
    public class OrderHistoryResponse
    {
        public List<UserOrderDto> Orders { get; set; } = new List<UserOrderDto>();
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int CancelledOrders { get; set; }
    }
}
