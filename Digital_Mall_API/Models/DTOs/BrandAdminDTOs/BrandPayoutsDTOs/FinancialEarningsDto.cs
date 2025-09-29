namespace Digital_Mall_API.Models.DTOs.BrandAdminDTOs.BrandPayoutsDTOs
{
    public class FinancialEarningsDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal PendingPayments { get; set; }
        public decimal CommissionDeductions { get; set; }
        public decimal NetEarnings { get; set; }

        public decimal AvailableForPayout { get; set; }
    }
}
