using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NordiskaPortal.Api.Models
{
    public enum TransactionStatus
    {
        Pending,
        Posted,
        Cancelled
    }

    public enum TransactionType
    {
        Deposit,
        Withdrawal,
        Interest,
        Tax,
        TransferIn,
        TransferOut
    }

    public class Transaction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int AccountId { get; set; }

        [ForeignKey(nameof(AccountId))]
        public SavingsAccount? SavingsAccount { get; set; }

        [Required]
        [MaxLength(20)]
        // "Deposit", "Withdrawal, Interest, Tax"
        public TransactionType Type { get; set; }

        // Only what the customer wrote, in any language, e.g. "Årsränta 2026".
        // null = nothing written; the frontend shows the type in the reader's language.
        [MaxLength(100)]
        public string? Description { get; set; }

        // Transfers only: the other account's number, stored as data (not text),
        // so the frontend can show the label in any language.
        // null for deposits, withdrawals, interest and tax.
        [MaxLength(20)]
        public string? CounterpartAccountNumber { get; set; }

        [Column(TypeName = "decimal(15,2)")]
        public decimal Amount { get; set; }

        public DateTime TransactionDate { get; set; }

        public DateTime PostingDate { get; set; }

        [Required]
        public TransactionStatus Status { get; set; } = TransactionStatus.Pending;


        public static DateTime CalculatePostingDate(DateTime transactionDate, int gapDays = 2)
        {
            var postingDate = transactionDate.AddDays(gapDays);
 
            if (postingDate.DayOfWeek == DayOfWeek.Saturday)
            {
                postingDate = postingDate.AddDays(2);
            }
            else if (postingDate.DayOfWeek == DayOfWeek.Sunday)
            {
                postingDate = postingDate.AddDays(1);
            }
            return postingDate;
        }
    }
}