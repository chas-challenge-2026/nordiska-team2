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
        Tax
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
        // "Deposit" or "Withdrawal"
        public TransactionType Type { get; set; }

        // Description which can be typed in any language, eg: Årsränta 2026
        [MaxLength(100)]
        public string Description { get; set; } = string.Empty;

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