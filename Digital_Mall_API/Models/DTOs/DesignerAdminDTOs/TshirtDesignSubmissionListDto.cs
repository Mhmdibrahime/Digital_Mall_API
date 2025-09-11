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


}
