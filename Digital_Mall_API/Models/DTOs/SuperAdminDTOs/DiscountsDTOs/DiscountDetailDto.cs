namespace Digital_Mall_API.Models.DTOs.SuperAdminDTOs.DiscountsDTOs
{
    public class DiscountDetailDto
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
      
    }
}
