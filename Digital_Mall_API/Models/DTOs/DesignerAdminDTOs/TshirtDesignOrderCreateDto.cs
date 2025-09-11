namespace Digital_Mall_API.Models.DTOs.DesignerAdminDTOs
{
    public class TshirtDesignOrderCreateDto
    {
        public string CustomerUserId { get; set; }
        public string ChosenColor { get; set; }
        public string ChosenStyle { get; set; }
        public string ChosenSize { get; set; }
        public string CustomerDescription { get; set; }
        public string CustomerImageUrl { get; set; }
    }


}
