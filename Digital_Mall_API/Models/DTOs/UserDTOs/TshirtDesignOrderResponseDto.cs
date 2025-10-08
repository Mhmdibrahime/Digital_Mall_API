namespace Digital_Mall_API.Models.DTOs.UserDTOs
{
    public class TshirtDesignOrderResponseDto
    {
        public int Id { get; set; }
        public string CustomerUserId { get; set; }
        public string ChosenColor { get; set; }
        public string ChosenStyle { get; set; }
        public string ChosenSize { get; set; }
        public string TshirtType { get; set; }
        public decimal Length { get; set; }
        public decimal Weight { get; set; }
        public string CustomerDescription { get; set; }

        // صور التيشيرت 4 حقول ثابتة
        public string TshirtFrontImageUrl { get; set; }
        public string TshirtBackImageUrl { get; set; }
        public string TshirtLeftImageUrl { get; set; }
        public string TshirtRightImageUrl { get; set; }

        // صور الديزاين كقائمة
        public List<string> CustomerImageUrls { get; set; } = new List<string>();

        public string FinalDesignUrl { get; set; }
        public string DesignerNotes { get; set; }
        public string Status { get; set; }
        public decimal FinalPrice { get; set; }
        public bool IsPaid { get; set; }
        public DateTime RequestDate { get; set; }

        public List<AddOrderTextDto> Texts { get; set; } = new List<AddOrderTextDto>();
    }

}
