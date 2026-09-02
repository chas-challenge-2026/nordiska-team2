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
        public string Type { get; set; } = string.Empty;
        // "deposit" or "withdrawal"

        [Column(TypeName = "decimal(15,2)")]
        public decimal Amount { get; set; }
        // always stored positive; sign applied by Type when summing for balance

        // [Column(TypeName = "decimal(15,2)")]
        // public decimal? BalanceAfter { get; set; }

        /*
            Removed BalanceAfter. 
            
            This was a stored point-in-time snapshot, same problem as SavingsAccount.Balance.
            It can drift from what SUM(Transactions) actually produces, which is exactly the 
            kind of derived/duplicated state the ledger pattern exists to eliminate. 
            
            If a historical balance is ever needed, compute it by summing Posted transactions 
            up to that point.
        */

        public DateTime TransactionDate { get; set; }

        public DateTime PostingDate { get; set; }

        [Required]
        public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

        /*
            Current balance = SUM of Amount (signed by Type) for all Transactions
            on this account where Status == Posted. 
            
            Pending transactions do not yet affect balance.
        */

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