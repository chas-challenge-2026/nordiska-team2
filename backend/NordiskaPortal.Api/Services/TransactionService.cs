using System.Data;
using Microsoft.EntityFrameworkCore;
using NordiskaPortal.Api.Data;
using NordiskaPortal.Api.DTOs;
using NordiskaPortal.Api.Models;

namespace NordiskaPortal.Api.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly BankContext _db;

        public TransactionService(BankContext db)
        {
            _db = db;
        }

        public async Task<decimal> GetBalanceAsync(int accountId)
        {
            return await _db.Transactions
                .Where(t => t.AccountId == accountId && t.Status == TransactionStatus.Posted)
                .SumAsync(t => t.Type == "Deposit" ? t.Amount : -t.Amount);
        }

        public async Task<TransactionResult> DepositAsync(int accountId, decimal amount)
        {
            /*
                A deposit is a pure insert so nothing is read then written back.
                Two concurrent deposits just become two independent rows.
                There's no shared mutable value for them to race over. 
                
                This is the core structural fix for v1's race condition.
            */

            if (amount <= 0)
                return new TransactionResult(false, "Beloppet måste vara större än 0.", null);

            var account = await _db.SavingsAccounts.FindAsync(accountId);
            if (account == null)
                return new TransactionResult(false, "Kontot kunde inte hittas.", null);

            var now = DateTime.UtcNow;
            var transaction = new Transaction
            {
                AccountId = accountId,
                Type = "Deposit",
                Amount = amount,
                TransactionDate = now,
                PostingDate = Transaction.CalculatePostingDate(now),
                Status = TransactionStatus.Posted
            };
            
            _db.Transactions.Add(transaction);
            await _db.SaveChangesAsync();

            return new TransactionResult(true, null, transaction);
        }

        public async Task<TransactionResult> WithdrawAsync(int accountId, decimal amount)
        {
            if (amount <= 0) 
            {
                return new TransactionResult(false, "Beloppet måste vara större än 0.", null);
            }
                
            /*
                Withdrawal has a race condition that deposit doesn't: 
                "is there enough balance" requires reading a derived 
                SUM before deciding whether to insert. 
                
                Two concurrent withdrawals could both read the same 
                balance and both pass the check, together overdrawing 
                the account.
                
                The same category of bug as v1, just moved from
                "stored balance column" to "insufficient funds check". 
                
                Wrapping the read + insert in a Serializable transaction makes 
                Postgres detect that conflict and fail one of the two attempts 
                instead of silently allowing both.
            */
            using var dbTransaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var account = await _db.SavingsAccounts.FindAsync(accountId);
                if (account == null)
                {
                    await dbTransaction.RollbackAsync();
                    return new TransactionResult(false, "Kontot kunde inte hittas.", null);
                }

                var currentBalance = await _db.Transactions
                    .Where(t => t.AccountId == accountId && t.Status == TransactionStatus.Posted)
                    .SumAsync(t => t.Type == "Deposit" ? t.Amount : -t.Amount);

                if (currentBalance < amount)
                {
                    await dbTransaction.RollbackAsync();
                    return new TransactionResult(false, "Otillräckligt saldo.", null);
                }

                var now = DateTime.UtcNow;
                var withdrawal = new Transaction
                {
                    AccountId = accountId,
                    Type = "Withdrawal",
                    Amount = amount,
                    TransactionDate = now,
                    PostingDate = Transaction.CalculatePostingDate(now),
                    Status = TransactionStatus.Posted
                };

                _db.Transactions.Add(withdrawal);
                await _db.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                return new TransactionResult(true, null, withdrawal);
            }
            catch (Exception)
            {
                /*
                    Postgres raises a serialization failure (SQLSTATE 40001) 
                    here when it detects the race described above. 
                    
                    A production system would typically catch that specific 
                    error and retry the whole operation automatically a few 
                    times before giving up. This catches broadly and just 
                    reports failure instead.

                    Simplified for now and not the full production-grade 
                    answer.
                */
                await dbTransaction.RollbackAsync();
                return new TransactionResult(false, "Transaktionen misslyckades på grund av samtidig åtkomst. Försök igen.", null);
            }
        }

        public async Task<List<LedgerEntryDto>> GetHistoryAsync(int accountId)
        {
            var transactions = await _db.Transactions
                .Where(t => t.AccountId == accountId)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();

            // Mapping happens here inside the service.
            // Controller and any client never see the raw Transaction entity or its internal fields (Status, PostingDate, etc).
            return transactions.Select(t => new LedgerEntryDto(
                Date: t.TransactionDate,
                Description: t.Type == "Deposit" ? "Insättning" : "Uttag",
                Amount: t.Type == "Deposit" ? t.Amount : -t.Amount
            )).ToList();
        }
    }
}