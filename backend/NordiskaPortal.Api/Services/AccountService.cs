using Microsoft.EntityFrameworkCore;
using NordiskaPortal.Api.Data;
using NordiskaPortal.Api.DTOs;

// Fetches a customer's accounts.
// Reusing ITransactionService.GetBalanceAsync for each one.
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
    }
}