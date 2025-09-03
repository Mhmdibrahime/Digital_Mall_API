using Digital_Mall_API.Models.Entities.User___Authentication;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Digital_Mall_API.Models.Entities.T_Shirt_Customization
{
    public class TshirtDesignOrder
    {
        public Guid Id { get; set; }

        [Required]
        public Guid CustomerUserId { get; set; }

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

        [Required]
        [Url]
        [StringLength(500)]
        public string CustomerImageUrl { get; set; }

        [Url]
        [StringLength(500)]
        public string FinalDesignUrl { get; set; }

        [StringLength(1000)]
        public string DesignerNotes { get; set; }

        [Required]
        [StringLength(20)]

        public string Status { get; set; }

        

        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal FinalPrice { get; set; }

        public DateTime? EstimatedDeliveryDate { get; set; }

        [Required]
        public bool IsPaid { get; set; } = false;

        public virtual ApplicationUser? CustomerUser { get; set; }
    }
}