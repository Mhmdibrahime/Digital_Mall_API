using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.Entities.T_Shirt_Customization
{
    public class TshirtDesignOrderImage
    {
        public int Id { get; set; }

        [Required]
        public int TshirtDesignOrderId { get; set; }
        public virtual TshirtDesignOrder Order { get; set; }

        [Required]
        [Url]
        [StringLength(500)]
        public string ImageUrl { get; set; }
    }
}