using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NordiskaPortal.Api.Models
{
    public class Notification
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int? CustomerId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Recipient { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty; // "deposit_confirmation"

        [MaxLength(50)]
        public string RefId { get; set; } = string.Empty; // Transaction id, string for flexibility

        [MaxLength(255)]
        public string Message { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Status { get; set; } = "pending"; // pending | sent | failed

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? SentAt { get; set; }

        public int RetryCount { get; set; } = 0;

        // Earliest time the worker may try (again) to send this row.
        // Null means "as soon as possible". Set after a failed attempt to implement backoff.
        public DateTime? NextAttemptAt { get; set; }
    }
}