namespace Digital_Mall_API.Models.DTOs.BrandAdminDTOs.BrandPayoutsDTOs
{
    public class PayoutSummaryDto
    {
        public int TotalPayouts { get; set; }
        public int PendingPayouts { get; set; }
        public int ApprovedPayouts { get; set; }
        public int CompletedPayouts { get; set; }
        public int RejectedPayouts { get; set; }
        public decimal TotalPayoutAmount { get; set; }
        public decimal PendingAmount { get; set; }
        public decimal ApprovedAmount { get; set; }
    }
}
