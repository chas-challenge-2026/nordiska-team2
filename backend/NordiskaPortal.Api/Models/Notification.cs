using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NordiskaPortal.Api.Models
{
    public class Notification
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Recipient { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty; // "deposit_confirmation"

        [MaxLength(50)]
        public string RefId { get; set; } = string.Empty; // Transaction id, string for flexibility

        [MaxLength(20)]
        public string Status { get; set; } = "pending"; // pending | sent | failed

        public DateTime? SentAt { get; set; }

        public int RetryCount { get; set; } = 0;
    }
}