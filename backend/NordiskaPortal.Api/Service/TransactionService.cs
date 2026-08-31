using Microsoft.EntityFrameworkCore;
using NordiskaPortal.Api.Data;
using NordiskaPortal.Api.Models;

namespace NordiskaPortal.Api.Service
{
    public class TransactionService
    {
        private readonly BankContext _context;

        public TransactionService(BankContext context)
        {
            _context = context;
        }

        // Enables customer to view all transactions associated with chosen account
        public List<Transaction> GetTransactionsForAccount(string accountNumber)
        {
            return _context.Transactions
                .Include(t => t.SavingsAccount)
                    .ThenInclude(sa => sa!.Customer)
                .Where(t => t.SavingsAccount!.AccountNumber == accountNumber)
                .OrderByDescending(t => t.TransactionDate)
                .ToList();
        }
    }
}