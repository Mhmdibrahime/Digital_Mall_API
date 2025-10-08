namespace Digital_Mall_API.Models.DTOs.UserDTOs
{
    public class AddTshirtDesignOrderDto
    {
        public string ChosenColor { get; set; }
        public string ChosenStyle { get; set; }
        public string ChosenSize { get; set; }
        public string TshirtType { get; set; }
        public decimal Length { get; set; }
        public decimal Weight { get; set; }
        public string CustomerDescription { get; set; }

        // صور التيشيرت 4 ملفات
        public IFormFile TshirtFrontImage { get; set; }
        public IFormFile TshirtBackImage { get; set; }
        public IFormFile TshirtLeftImage { get; set; }
        public IFormFile TshirtRightImage { get; set; }

        // صور الديزاين كملفات متعددة
        public List<IFormFile> CustomerImages { get; set; } = new List<IFormFile>();

     
    }
}
