using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NordiskaPortal.Api.Models
{
    public class AuditEntry
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Actor { get; set; } = string.Empty; // customer id or "system"

        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty; // "login", "deposit", "tax_report_generated", etc.

        [MaxLength(50)]
        public string RefId { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public string? Signature { get; set; } // Populated for tax-report generation via the native pdf_signer module.
    }
}