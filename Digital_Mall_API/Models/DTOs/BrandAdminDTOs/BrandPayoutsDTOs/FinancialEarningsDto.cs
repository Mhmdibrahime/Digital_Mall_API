namespace Digital_Mall_API.Models.DTOs.BrandAdminDTOs.BrandPayoutsDTOs
{
    public class FinancialEarningsDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal PendingPayments { get; set; }
        public decimal PlatformCommissionDeductions { get; set; }
        public decimal ModelCommissionDeductions { get; set; }
        public decimal PendingModelCommissions { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal NetEarnings { get; set; }
        public decimal AvailableForPayout { get; set; }
    }
}
