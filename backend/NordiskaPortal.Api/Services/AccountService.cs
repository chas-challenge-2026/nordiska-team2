using Microsoft.EntityFrameworkCore;
using NordiskaPortal.Api.Data;
using NordiskaPortal.Api.DTOs;
using NordiskaPortal.Api.Models;
using System.Data;
using System.Security.Cryptography;

namespace NordiskaPortal.Api.Services
{
    public class AccountService : IAccountService
    {
        // The bank's standard rate for new accounts. Set by the server.
        private const decimal DefaultInterestRate = 0.0350m;

        private const int MaxOpenAccounts = 10;

        private readonly BankContext _db;
        private readonly ITransactionService _transactionService;
        private readonly IAuditService _audit;  
 
        public AccountService(BankContext db, ITransactionService transactionService, IAuditService audit)
        {
            _db = db;
            _transactionService = transactionService;
             _audit = audit;  
        }
 
        public async Task<List<AccountDto>> GetAccountsForCustomerAsync(int customerId)
        {
            var accounts = await _db.SavingsAccounts
                .Where(a => a.CustomerId == customerId && a.ClosedAt == null)
                .ToListAsync();
 
            var result = new List<AccountDto>();
            foreach (var account in accounts)
            {
                var balance = await _transactionService.GetBalanceAsync(account.Id);
                result.Add(new AccountDto(account.Id, account.AccountNumber, account.AccountType, account.InterestRate, balance));
            }
 
            return result;
        }
 
        public async Task<bool> CustomerOwnsAccountAsync(int customerId, int accountId)
        {
            return await _db.SavingsAccounts.AnyAsync(a => a.Id == accountId && a.CustomerId == customerId && a.ClosedAt == null);
        }

        public async Task<FinancialSummaryDto> GetFinancialSummaryAsync(
            int customerId, DateTime from, DateTime to)
        {
            var accountIds = await _db.SavingsAccounts
                .Where(a => a.CustomerId == customerId)
                .Select(a => a.Id)
                .ToListAsync();

            var transactions = await _db.Transactions
                .Where(t => accountIds.Contains(t.AccountId)
                         && t.Status == TransactionStatus.Posted
                         && t.TransactionDate >= from
                         && t.TransactionDate <= to)
                .ToListAsync();

            var income = transactions
                .Where(t => t.Type == TransactionType.Deposit
                         || t.Type == TransactionType.Interest)
                .Sum(t => t.Amount);

            var expenses = transactions
                .Where(t => t.Type == TransactionType.Withdrawal
                         || t.Type == TransactionType.Tax)
                .Sum(t => t.Amount);

            return new FinancialSummaryDto(income, expenses, from, to);
        }

        public async Task<AccountResult> OpenAccountAsync(int customerId)
        {
            var openCount = await _db.SavingsAccounts
                .CountAsync(a => a.CustomerId == customerId && a.ClosedAt == null);

            if (openCount >= MaxOpenAccounts)
                return new AccountResult(false, $"Du kan ha högst {MaxOpenAccounts} öppna konton.", null);

            // Random numbers make collisions very unlikely and the unique index makes them impossible. 
            // Discard and try a new number in case it happens.
            for (var attempt = 1; attempt <= 3; attempt++)
            {
                var account = new SavingsAccount
                {
                    CustomerId = customerId,
                    AccountNumber = $"NKM-{RandomNumberGenerator.GetInt32(10_000_000, 100_000_000)}",
                    InterestRate = DefaultInterestRate,
                    AccountType = "Savings",
                    CreatedAt = DateTime.UtcNow
                };

                _db.SavingsAccounts.Add(account);
                _audit.Record(AuditActions.AccountOpened, customerId, account.AccountNumber);

                try
                {
                    await _db.SaveChangesAsync();
                    var dto = new AccountDto(account.Id, account.AccountNumber, account.AccountType, account.InterestRate, 0m);
                    return new AccountResult(true, null, dto);
                }
                catch (DbUpdateException ex) when (PostgresErrors.IsUniqueViolation(ex))
                {
                    // Un-stage both the account and its audit row before retrying,
                    // or the next save would try to insert them again.
                    _db.Entry(account).State = EntityState.Detached;
                    foreach (var staged in _db.ChangeTracker.Entries<AuditEntry>()
                                .Where(e => e.State == EntityState.Added).ToList())
                    {
                        staged.State = EntityState.Detached;
                    }
                }
            }

            throw new InvalidOperationException("Could not generate a unique account number after 3 attempts.");
        }

        public async Task<AccountResult> CloseAccountAsync(int customerId, int accountId)
        {
            /*
                Same race as a withdrawal: "is the balance zero" reads a derived SUM
                before acting on it. Serializable makes Postgres abort the close if a
                concurrent transfer touches the account mid-check.
            */
            using var dbTransaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                // Ownership checked here too, not only in the controller.
                var account = await _db.SavingsAccounts.FirstOrDefaultAsync(a =>
                    a.Id == accountId && a.CustomerId == customerId && a.ClosedAt == null);

                if (account == null)
                {
                    await dbTransaction.RollbackAsync();
                    return new AccountResult(false, "Kontot kunde inte hittas.", null);
                }

                var balance = await _transactionService.GetBalanceAsync(accountId);
                if (balance != 0m)
                {
                    await dbTransaction.RollbackAsync();
                    return new AccountResult(false,
                        "Kontot måste vara tomt innan det kan avslutas. Flytta pengarna till ett annat konto först.", null);
                }

                // Goals linked to this account are removed with it. 
                // Each removal gets its own audit row, in the same transaction as the close, so the customer's activity log shows exactly which goals disappeared and why.
                var linkedGoals = await _db.SavingsGoals
                    .Where(g => g.AccountId == accountId && g.CustomerId == customerId)
                    .ToListAsync();

                foreach (var goal in linkedGoals)
                {
                    _db.SavingsGoals.Remove(goal);
                    _audit.Record(AuditActions.SavingsGoalDeleted, customerId, goal.Id.ToString());
                }

                account.ClosedAt = DateTime.UtcNow;
                _audit.Record(AuditActions.AccountClosed, customerId, account.AccountNumber);

                await _db.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                return new AccountResult(true, null, null);
            }
            catch (Exception ex) when (PostgresErrors.IsSerializationFailure(ex))
            {
                return new AccountResult(false, "Kontot ändrades samtidigt. Försök igen.", null);
            }
        }
    }
}