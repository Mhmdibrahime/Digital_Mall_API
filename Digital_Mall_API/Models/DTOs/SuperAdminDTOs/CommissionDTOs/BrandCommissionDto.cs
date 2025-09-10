namespace Digital_Mall_API.Models.DTOs.SuperAdminDTOs.CommissionDTOs
{
    public class BrandCommissionDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public decimal EffectiveCommissionRate { get; set; }
      
        public bool HasCustomRate { get; set; }
    }
}
