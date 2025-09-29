namespace Digital_Mall_API.Models.DTOs.BrandAdminDTOs.BrandPayoutsDTOs
{
    public class BrandEarningsDto
    {
        public string BrandId { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public decimal PendingPayments { get; set; }
        public decimal CommissionDeductions { get; set; }
        public decimal NetEarnings => TotalRevenue - CommissionDeductions;
        public decimal AvailableForPayout { get; set; }
    }
}
