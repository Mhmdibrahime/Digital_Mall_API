using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.Entities.Logs
{
    public class Log
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(50)]
        public string Level { get; set; } = "Information"; // Information, Warning, Error, Debug

        [Required]
        [MaxLength(255)]
        public string Source { get; set; } = "System";

        [Required]
        public string Message { get; set; }

        public string? Details { get; set; }

        [MaxLength(100)]
        public string? OrderId { get; set; }

        [MaxLength(100)]
        public string? PaymobOrderId { get; set; }

        [MaxLength(50)]
        public string? TransactionId { get; set; }

        [MaxLength(1000)]
        public string? AdditionalData { get; set; }
    }
}
