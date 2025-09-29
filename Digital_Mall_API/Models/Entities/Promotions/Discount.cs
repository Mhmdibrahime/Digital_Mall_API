using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.Entities.Promotions
{
    public class Discount
    {
        public int Id { get; set; }

        

        [StringLength(1000)]
        public string Description { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Active"; 

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

    }
    
}