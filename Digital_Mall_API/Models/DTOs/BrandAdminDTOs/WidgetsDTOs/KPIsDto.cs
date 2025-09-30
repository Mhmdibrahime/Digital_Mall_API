namespace Digital_Mall_API.Models.DTOs.BrandAdminDTOs.WidgetsDTOs
{
    public class KPIsDto
    {
        public TotalProductsDto TotalProducts { get; set; } = new TotalProductsDto();
        public TotalOrdersDto TotalOrders { get; set; } = new TotalOrdersDto();
        public TotalRevenueDto TotalRevenue { get; set; } = new TotalRevenueDto();
        public ActiveDiscountsDto ActiveDiscounts { get; set; } = new ActiveDiscountsDto();
        public AverageOrderPriceDto AverageOrderPrice { get; set; } = new AverageOrderPriceDto();
    }
}
