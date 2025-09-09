namespace Digital_Mall_API.Models.DTOs.SuperAdminDTOs.ModelsManagementDTOs
{
    public class ReelInfoDto
    {
        public Guid Id { get; set; }
        public string Caption { get; set; }
        public int LikesCount { get; set; }
        public DateTime PostedDate { get; set; }
        public int DurationInSeconds { get; set; }
    }
}
