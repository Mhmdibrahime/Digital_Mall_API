namespace Digital_Mall_API.Models.DTOs.DesignerAdminDTOs
{
    public class TshirtDesignSubmissionListDto
    {
        public int SubmissionId { get; set; }
        public string ClientName { get; set; }
        public string DesignName { get; set; }
        public string Description { get; set; }
        public DateTime SubmissionDate { get; set; }
        public List<string> ImageUrls { get; set; }
    }
    public class TshirtTemplateDto
    {
        public string Name { get; set; }

        public IFormFile? SizeChart { get; set; }
        public IFormFile? FrontImage { get; set; }
        public IFormFile? BackImage { get; set; }
        public IFormFile? LeftImage { get; set; }
        public IFormFile? RightImage { get; set; }
    }

    public class TshirtTemplateResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public string? SizeChartUrl { get; set; }
        public string? FrontImageUrl { get; set; }
        public string? BackImageUrl { get; set; }
        public string? LeftImageUrl { get; set; }
        public string? RightImageUrl { get; set; }
    }

    public class TShirtSizeDto
    {
        public string Name { get; set; }
    }

    public class TShirtStyleDto
    {
        public string Name { get; set; }
    }
}
