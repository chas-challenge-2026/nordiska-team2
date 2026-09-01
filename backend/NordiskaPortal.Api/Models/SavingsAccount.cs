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

        // [Column(TypeName = "decimal(15,2)")]
        // public decimal Balance { get; set; } = 0;

        /* 
        Removed stored Balance field. 
        
        Balance should always be derived as SUM(Transaction.Amount) over 
        Posted transactions for this account, computed in the service 
        layer so it is never stored and never mutated directly. 
        
        This is structural fix for the v1 race condition, where
        concurrent UPDATE statements on a stored balance column caused 
        corruption.
        */

        [Column(TypeName = "decimal(5,4)")]
        public decimal InterestRate { get; set; } = 0.0350m;

        [MaxLength(20)]
        public string AccountType { get; set; } = "Savings";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public List<Transaction> Transactions { get; set; } = new();
    }
}