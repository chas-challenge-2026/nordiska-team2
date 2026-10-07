using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NordiskaPortal.Api.Models
{
    // One row per security- or money-relevant event. 
    // Append-only: nothing in the application updates or deletes these rows, and there is deliberately no endpoint that could.
    // Permanent record of who did what, lives in the database next to the data it describes.
    public class AuditEntry
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // Who performed the action: 
        // a customer id, "system" for background jobs (e.g. interest accrual), or "anonymous" for a failed login that didn't match any customer.
        [Required]
        [MaxLength(50)]
        public string Actor { get; set; } = string.Empty;

        // Whose data the event concerns. Usually the same as Actor, but kept separate because they can differ: 
        // a failed login against Anna's email has an unknown actor but concerns Anna, and a "system" interest 
        // payment concerns a specific customer.
        // This is the column the read path filters on.
        public int? CustomerId { get; set; }

        // One of the constants in AuditActions.
        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty;

        // What the action touched, e.g. an account number or a transaction id.
        [MaxLength(50)]
        public string? RefId { get; set; }

        public DateTime Timestamp { get; set; }

        // IPv6 address in text form.
        [MaxLength(45)]
        public string? IpAddress { get; set; }

        // HttpContext.TraceIdentifier
        // The same id GlobalExceptionHandler returns to the client and Serilog writes to its logs. 
        // Lets you go from an audit row straight to the log lines of that request.
        [MaxLength(100)]
        public string? CorrelationId { get; set; }

        // Populated for tax-report generation via the native pdf_signer module. 
        // (Unused by everything else)
        public string? Signature { get; set; }
    }
}