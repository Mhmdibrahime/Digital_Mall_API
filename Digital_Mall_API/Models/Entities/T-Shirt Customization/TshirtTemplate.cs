using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.Entities.T_Shirt_Customization
{
    public class TshirtTemplate
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public string? SizeChartUrl { get; set; }
        public string? FrontImageUrl { get; set; }
        public string? BackImageUrl { get; set; }
        public string? LeftImageUrl { get; set; }
        public string? RightImageUrl { get; set; }
    }
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
        public string FontSize { get; set; } = "M"; 

        [StringLength(50)]
        public string FontStyle { get; set; } = "Normal"; 
    }
}