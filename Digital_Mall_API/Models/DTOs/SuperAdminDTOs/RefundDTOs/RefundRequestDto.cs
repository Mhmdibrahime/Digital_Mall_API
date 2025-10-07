namespace Digital_Mall_API.Models.DTOs.SuperAdminDTOs.RefundDTOs
{
    public class RefundRequestDto
    {
        public int Id { get; set; }
        public string RefundNumber { get; set; }
        public int OrderId { get; set; }
        public int OrderItemId { get; set; }
        public string CustomerName { get; set; }
        public string BrandName { get; set; }
        public string ProductName { get; set; }
        public string OrderDate { get; set; }
        public string Reason { get; set; }
        public string ImageUrl { get; set; }
        public string Status { get; set; }
        public string RequestDate { get; set; }
    }
}
