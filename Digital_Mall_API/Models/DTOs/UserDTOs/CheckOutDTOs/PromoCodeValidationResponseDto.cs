namespace Digital_Mall_API.Models.DTOs.UserDTOs.CheckOutDTOs
{
    public class PromoCodeValidationResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int? PromoCodeId { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal OriginalTotal { get; set; }
        public decimal FinalTotal { get; set; }
        public List<OrderItemDiscountDto> ApplicableItems { get; set; }
    }
}
