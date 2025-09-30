namespace Digital_Mall_API.Models.DTOs.BrandAdminDTOs.BrandPayoutsDTOs
{
    public class PayoutDto
    {
        public int Id { get; set; }
        public string PayoutId { get; set; } = string.Empty;
        public string PayeeUserId { get; set; }
        public string PayeeName { get; set; } = string.Empty;
        public string PayeeEmail { get; set; } = string.Empty;
        public string PayeeType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string BankAccountNumber { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
