using Digital_Mall_API.Models.Entities.Reels___Content;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Digital_Mall_API.Models.Entities.Logs
{
    public class WebhookLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime LogDate { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(50)]
        public string LogLevel { get; set; } // "INFO", "WARNING", "ERROR", "DEBUG"

        [Required]
        [MaxLength(100)]
        public string WebhookType { get; set; }

  

       

        [MaxLength(1500)]
        public string Message { get; set; }

        public string Details { get; set; } // JSON or detailed message

       


        
    }
}
