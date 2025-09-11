namespace Digital_Mall_API.Models.DTOs.DesignerAdminDTOs
{
    public class TshirtDesignSubmissionCreateDto
    {
        public int OrderId { get; set; }
        public string DesignName { get; set; }
        public string Description { get; set; }
        public List<IFormFile> DesignImages { get; set; }
    }


}
