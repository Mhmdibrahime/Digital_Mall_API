using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.Entities.User___Authentication
{
    public class TshirtDesigner
    {
        public string Id { get; set; }
        [StringLength(500)]
        public string FullName { get; set; }
        [StringLength(50)]
        public string Status { get; set; } = "Active"; 
        [StringLength(100)]
        public string Password { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}