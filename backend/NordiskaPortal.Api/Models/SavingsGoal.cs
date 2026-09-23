using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NordiskaPortal.Api.Models
{
    // A customer can have any number of goals. 
    // AccountId is optional: when set, progress is that account's live ledger balance.
    public class SavingsGoal
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public Customer? Customer { get; set; }

        public int? AccountId { get; set; }

        [ForeignKey(nameof(AccountId))]
        public SavingsAccount? Account { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public decimal TargetAmount { get; set; }

        public DateTime? Deadline { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}