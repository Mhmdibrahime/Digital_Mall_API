using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.Entities.T_Shirt_Customization
{
    public class TshirtDesignSubmission
    {
        [Key]
        public int Id { get; set; }  

        [Required]
        public int OrderId { get; set; }
        public TshirtDesignOrder Order { get; set; }

        [Required]
        [StringLength(2000)]
        public string DesignName { get; set; }
        [StringLength(2000)]
        public string Description { get; set; }

        public DateTime SubmissionDate { get; set; } = DateTime.UtcNow;

        public virtual ICollection<TshirtDesignSubmissionImage> Images { get; set; }
    }
}