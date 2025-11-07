using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.DTOs.UserDTOs.ReelsDTOs
{
    public class ReportReelRequest
    {
        [Required]
        public int ReelId { get; set; }

        [Required]
        public string Reason { get; set; }

        public string AdditionalDetails { get; set; }
    }
}
