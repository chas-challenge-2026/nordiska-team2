using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NordiskaPortal.Api.Models
{
    public class SavingsAccount
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public Customer? Customer { get; set; }

        [Required]
        [MaxLength(20)]
        public string AccountNumber { get; set; } = string.Empty;

        [Column(TypeName = "decimal(5,4)")]
        public decimal InterestRate { get; set; } = 0.0350m;

        [MaxLength(20)]
        public string AccountType { get; set; } = "Savings";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Null = open. 
        // Accounts are closed, never deleted because the ledger history must remain (transactions are immutable) and audit rows refer to the account number.
        public DateTime? ClosedAt { get; set; }
    }
}