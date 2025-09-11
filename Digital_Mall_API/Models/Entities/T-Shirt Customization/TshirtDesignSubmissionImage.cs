using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.Entities.T_Shirt_Customization
{
    public class TshirtDesignSubmissionImage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SubmissionId { get; set; }
        public TshirtDesignSubmission Submission { get; set; }

        [Required]
        [StringLength(500)]
        public string ImageUrl { get; set; }
    }
}