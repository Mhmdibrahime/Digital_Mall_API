namespace Digital_Mall_API.Models.DTOs.SuperAdminDTOs.PayoutsDTOs
{
    public class PayoutDto
    {
        public int Id { get; set; }
        public string PayeeName { get; set; }
        public string PayeeEmail { get; set; }
        public string PayeeType { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public string BankAccount { get; set; }
    }
}
