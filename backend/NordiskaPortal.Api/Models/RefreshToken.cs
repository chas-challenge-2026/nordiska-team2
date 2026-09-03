using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Stores hashed refresh tokens server-side so sessions can actually be revoked
// Fix for v1's "logout doesn't end the session" bug.
namespace NordiskaPortal.Api.Models
{
    public class RefreshToken
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public Customer? Customer { get; set; }

        [Required]
        [MaxLength(100)]
        public string TokenHash { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RevokedAt { get; set; }

        [NotMapped]
        public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpiresAt;
    }
}