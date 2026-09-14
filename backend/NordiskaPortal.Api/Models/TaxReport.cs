using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NordiskaPortal.Api.Models
{
    public class TaxReport
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int AccountId { get; set; }

        [ForeignKey(nameof(AccountId))]
        public SavingsAccount? SavingsAccount { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        [MaxLength(30)]
        public string ReportId { get; set; } = string.Empty;

        [Required]
        public byte[] PdfData { get; set; } = Array.Empty<byte>();

        public DateTime GeneratedAt { get; set; }
    }
}
