using System.Data;
using System.Linq.Expressions;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using NordiskaPortal.Api.Data;
using NordiskaPortal.Api.DTOs;
using NordiskaPortal.Api.Models;

namespace NordiskaPortal.Api.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly BankContext _db;
        private readonly IAuditService _audit; 

        public TransactionService(BankContext db, IAuditService audit)
        {
            _db = db;
            _audit = audit;
        }

        // Definition of which types add money and which remove it.
        // An Expression so EF can translate it to SQL inside SumAsync.
        // The compiled version below is the same rule for in-memory use.
        private static readonly Expression<Func<Transaction, decimal>> SignedAmountExpr = t =>
            t.Type == TransactionType.Deposit || t.Type == TransactionType.Interest || t.Type == TransactionType.TransferIn
                ? t.Amount
                : t.Type == TransactionType.Withdrawal || t.Type == TransactionType.Tax || t.Type == TransactionType.TransferOut
                    ? -t.Amount
                    : 0m;

        private static readonly Func<Transaction, decimal> SignedAmountCompiled = SignedAmountExpr.Compile();

        private static decimal GetSignedAmount(Transaction transaction) => SignedAmountCompiled(transaction);

        // Default description if no default description is passed
        private static string DefaultDescription(TransactionType type) => type switch
        {
            TransactionType.Deposit => "Insättning",
            TransactionType.Withdrawal => "Uttag",
            TransactionType.Interest => "Ränta",
            TransactionType.Tax => "Skatt",
            TransactionType.TransferIn => "Överföring",
            TransactionType.TransferOut => "Överföring",
            _ => "Transaktion"
        };

        private static string ResolveDescription(string? description, TransactionType type) => string.IsNullOrWhiteSpace(description) ? DefaultDescription(type) : description.Trim();
        public async Task<decimal> GetBalanceAsync(int accountId) 
        { 
            var transactions = await _db.Transactions
                                        .Where(t => t.AccountId == accountId && t.Status == TransactionStatus.Posted).ToListAsync(); 
            
            return await _db.Transactions
                            .Where(t => t.AccountId == accountId && t.Status == TransactionStatus.Posted)
                            .SumAsync(SignedAmountExpr);
        }

        // DEPOSIT
        public async Task<TransactionResult> DepositAsync(int accountId, decimal amount, string? description = null)
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
            if (account == null || account.ClosedAt != null)
                return new TransactionResult(false, "Kontot kunde inte hittas.", null);

            var now = DateTime.UtcNow;

            var transaction = new Transaction
            {
                AccountId = accountId,
                Type = TransactionType.Deposit,
                Description = ResolveDescription(description, TransactionType.Deposit),
                Amount = amount,
                TransactionDate = now,
                PostingDate = Transaction.CalculatePostingDate(now),
                Status = TransactionStatus.Posted
            };

            _db.Transactions.Add(transaction);
            _audit.Record(AuditActions.Deposit, account.CustomerId, account.AccountNumber);
            await _db.SaveChangesAsync();

            var entry = new LedgerEntryDto(transaction.TransactionDate, transaction.Description, transaction.Amount);
            return new TransactionResult(true, null, entry);
        }

        // WITHDRAW
        public async Task<TransactionResult> WithdrawAsync(int accountId, decimal amount, string? description = null)
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
                    .SumAsync(SignedAmountExpr);

                if (currentBalance < amount)
                {
                    await dbTransaction.RollbackAsync();
                    return new TransactionResult(false, "Otillräckligt saldo.", null);
                }

                var now = DateTime.UtcNow;
                var withdrawal = new Transaction
                {
                    AccountId = accountId,
                    Type = TransactionType.Withdrawal,
                    Description = ResolveDescription(description, TransactionType.Withdrawal),
                    Amount = amount,
                    TransactionDate = now,
                    PostingDate = Transaction.CalculatePostingDate(now),
                    Status = TransactionStatus.Posted
                };

                _db.Transactions.Add(withdrawal);
                _audit.Record(AuditActions.Withdrawal, account.CustomerId, account.AccountNumber);
                await _db.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                var entry = new LedgerEntryDto(withdrawal.TransactionDate, withdrawal.Description, -withdrawal.Amount);
                return new TransactionResult(true, null, entry);
            }
            catch (Exception ex) when (PostgresErrors.IsSerializationFailure(ex))
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
                return new TransactionResult(false, "Transaktionen misslyckades på grund av samtidig åtkomst. Försök igen.", null);
            }
        }

        // GET HISTORY
        public async Task<List<LedgerEntryDto>> GetHistoryAsync(int accountId)
        {
            var transactions = await _db.Transactions
                .Where(t => t.AccountId == accountId)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();

            return transactions.Select(t => new LedgerEntryDto(
                Date: t.TransactionDate,
                Description: ResolveDescription(t.Description, t.Type),
                Amount: GetSignedAmount(t)
            )).ToList();
        }

        // TRANSFER BETWEEN OWN ACCOUNTS
        public async Task<TransactionResult> TransferAsync(int fromAccountId, int toAccountId, decimal amount, string? description = null)
        {
            if (amount <= 0)
                return new TransactionResult(false, "Beloppet måste vara större än 0.", null);

            if (fromAccountId == toAccountId)
                return new TransactionResult(false, "Från- och tillkonto måste vara olika.", null);

            /*
                Same race as a withdrawal: "is there enough money" reads a derived
                SUM before inserting. Serializable makes Postgres fail one of two
                overlapping transfers instead of letting both overdraw the account.
            */
            using var dbTransaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var from = await _db.SavingsAccounts.FindAsync(fromAccountId);
                var to = await _db.SavingsAccounts.FindAsync(toAccountId);

                // Same message whether an account doesn't exist or isn't the caller's. (Never reveal which account numbers exist.)
                // The CustomerId check repeats the controller's ownership check on purpose:
                // "own accounts only" is enforced here too, so a future caller that forgets 
                // the controller check still can't move money to someone else's account.

                if (from == null || to == null || from.CustomerId != to.CustomerId || from.ClosedAt != null || to.ClosedAt != null)
                {
                    await dbTransaction.RollbackAsync();
                    return new TransactionResult(false, "Kontot kunde inte hittas.", null);
                }

                var balance = await _db.Transactions
                    .Where(t => t.AccountId == fromAccountId && t.Status == TransactionStatus.Posted)
                    .SumAsync(SignedAmountExpr);

                if (balance < amount)
                {
                    await dbTransaction.RollbackAsync();
                    return new TransactionResult(false, "Otillräckligt saldo.", null);
                }

                var now = DateTime.UtcNow;
                var postingDate = Transaction.CalculatePostingDate(now);
                var custom = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

                var outgoing = new Transaction
                {
                    AccountId = from.Id,
                    Type = TransactionType.TransferOut,
                    Description = custom ?? $"Överföring till {to.AccountNumber}",
                    Amount = amount,
                    TransactionDate = now,
                    PostingDate = postingDate,
                    Status = TransactionStatus.Posted
                };

                var incoming = new Transaction
                {
                    AccountId = to.Id,
                    Type = TransactionType.TransferIn,
                    Description = custom ?? $"Överföring från {from.AccountNumber}",
                    Amount = amount,
                    TransactionDate = now,
                    PostingDate = postingDate,
                    Status = TransactionStatus.Posted
                };

                _db.Transactions.AddRange(outgoing, incoming);
                _audit.Record(AuditActions.Transfer, from.CustomerId, $"{from.AccountNumber}>{to.AccountNumber}");

                // One save: both ledger rows and the audit row commit together.
                // Money can never leave one account without arriving in the other.
                await _db.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                var entry = new LedgerEntryDto(outgoing.TransactionDate, outgoing.Description, -outgoing.Amount);
                return new TransactionResult(true, null, entry);
            }
            catch (Exception ex) when (PostgresErrors.IsSerializationFailure(ex))
            {
                return new TransactionResult(false, "Transaktionen misslyckades på grund av samtidig åtkomst. Försök igen.", null);
            }
        }
    }
}