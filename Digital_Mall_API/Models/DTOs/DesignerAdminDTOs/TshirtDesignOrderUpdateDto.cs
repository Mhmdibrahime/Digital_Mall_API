namespace Digital_Mall_API.Models.DTOs.DesignerAdminDTOs
{
    public class TshirtDesignOrderUpdateDto
    {
        public string ChosenColor { get; set; }
        public string ChosenStyle { get; set; }
        public string ChosenSize { get; set; }
        public string CustomerDescription { get; set; }
        public string CustomerImageUrl { get; set; }
        public string FinalDesignUrl { get; set; }
        public string DesignerNotes { get; set; }
        public string Status { get; set; }
        public decimal FinalPrice { get; set; }
        public DateTime? EstimatedDeliveryDate { get; set; }
        public bool IsPaid { get; set; }
    }


}
