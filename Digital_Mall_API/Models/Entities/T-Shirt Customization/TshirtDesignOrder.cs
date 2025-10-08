using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Digital_Mall_API.Models.Entities.User___Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Digital_Mall_API.Models.Entities.T_Shirt_Customization;

public class TshirtDesignOrder
{
    public int Id { get; set; }

    [Required]
    public string CustomerUserId { get; set; }

    [Required]
    [StringLength(50)]
    public string ChosenColor { get; set; }

    [Required]
    [StringLength(50)]
    public string ChosenStyle { get; set; }

    [Required]
    [StringLength(10)]
    public string ChosenSize { get; set; }

    [Required]
    [StringLength(2000)]
    public string CustomerDescription { get; set; }

    [StringLength(50)]
    public string TshirtType { get; set; }
    public decimal Length { get; set; }
    public decimal Weight { get; set; }

   
    [Url]
    [StringLength(500)]
    public string TshirtFrontImage { get; set; }

    [Url]
    [StringLength(500)]
    public string TshirtBackImage { get; set; }

    [Url]
    [StringLength(500)]
    public string TshirtLeftImage { get; set; }

    [Url]
    [StringLength(500)]
    public string TshirtRightImage { get; set; }

    
    public virtual ICollection<TshirtDesignOrderImage> Images { get; set; } = new List<TshirtDesignOrderImage>();

    [StringLength(1000)]
    public string? DesignerNotes { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; }

    [Range(0, double.MaxValue)]
    [Column(TypeName = "decimal(18,2)")]
    public decimal FinalPrice { get; set; }
    
    public DateTime? EstimatedDeliveryDate { get; set; }

    [Required]
    public bool IsPaid { get; set; } = false;

    public DateTime RequestDate { get; set; } = DateTime.UtcNow;
    public virtual ICollection<TshirtOrderText> Texts { get; set; } = new List<TshirtOrderText>();
    public virtual Customer? CustomerUser { get; set; }
    public string? FinalDesignUrl { get; internal set; }
}
