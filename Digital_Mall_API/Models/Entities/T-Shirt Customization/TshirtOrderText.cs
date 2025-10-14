using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.Entities.T_Shirt_Customization
{
    public class TshirtOrderText
    {
        public int Id { get; set; }

        [Required]
        public int TshirtDesignOrderId { get; set; }
        public virtual TshirtDesignOrder Order { get; set; }

        [Required]
        [StringLength(500)]
        public string Text { get; set; }

        [StringLength(50)]
        public string FontFamily { get; set; } = "Arial";

        [StringLength(20)]
        public string FontColor { get; set; } = "#000000"; 

        [StringLength(10)]
        public int FontSize { get; set; } = 0; 

        [StringLength(50)]
        public string FontStyle { get; set; } = "Normal"; 
    }
}