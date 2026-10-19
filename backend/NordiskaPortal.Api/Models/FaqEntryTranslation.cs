using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NordiskaPortal.Api.Models
{
    public class FaqEntryTranslation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int FaqEntryId { get; set; }

        [ForeignKey(nameof(FaqEntryId))]
        public FaqEntry? FaqEntry { get; set; }

        // Two-letter code, matching Localization:SupportedLanguages ("en").
        [Required]
        [MaxLength(10)]
        public string Language { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Question { get; set; } = string.Empty;

        [Required]
        public string Answer { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        public string[] Keywords { get; set; } = Array.Empty<string>();
    }
}