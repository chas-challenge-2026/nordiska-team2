using Microsoft.EntityFrameworkCore;
using NordiskaPortal.Api.Data;
using NordiskaPortal.Api.DTOs;
using NordiskaPortal.Api.Models;

namespace NordiskaPortal.Api.Services
{
    public class AccountService : IAccountService
    {
        private readonly BankContext _db;
        private readonly ITransactionService _transactionService;
 
        public AccountService(BankContext db, ITransactionService transactionService)
        {
            _db = db;
            _transactionService = transactionService;
        }
 
        public async Task<List<AccountDto>> GetAccountsForCustomerAsync(int customerId)
        {
            var accounts = await _db.SavingsAccounts
                .Where(a => a.CustomerId == customerId)
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
            return await _db.SavingsAccounts.AnyAsync(a => a.Id == accountId && a.CustomerId == customerId);
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
    }
}