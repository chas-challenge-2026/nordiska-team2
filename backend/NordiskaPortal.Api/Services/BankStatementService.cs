using Microsoft.EntityFrameworkCore;
using NordiskaPortal.Api.Data;
using NordiskaPortal.Api.DTOs;
using NordiskaPortal.Api.Models;

namespace NordiskaPortal.Api.Services
{
    public class BankStatementService
    {
        private readonly BankContext _db;
        private readonly TaxReportService _taxReportService;

        // Reuses TaxReportService.GetBalanceAsOfAsync (marked internal),
        // so this service depends on that one for the opening-balance
        // calculation instead of duplicating the same query.
        public BankStatementService(BankContext db, TaxReportService taxReportService)
        {
            _db = db;
            _taxReportService = taxReportService;
        }

        /// <summary>
        /// Builds a statement for a full calendar month, e.g. year=2026,
        /// month=10 covers 2026-10-01 up to (not including) 2026-11-01.
        /// </summary>
        public async Task<BankStatementDto?> BuildStatementForMonthAsync(int accountId, int year, int month)
        {
            if (month < 1 || month > 12)
                throw new ArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12.");

            var periodStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var periodEnd = periodStart.AddMonths(1);

            return await BuildStatementAsync(accountId, periodStart, periodEnd);
        }

        public async Task<BankStatementDto?> BuildStatementAsync(int accountId, DateTime periodStart, DateTime periodEnd)
        {
            var account = await _db.SavingsAccounts
                .Include(a => a.Customer)
                .FirstOrDefaultAsync(a => a.Id == accountId);

            if (account == null || account.Customer == null)
                return null;

            decimal openingBalance = await _taxReportService.GetBalanceAsOfAsync(accountId, periodStart);

            var transactions = await _db.Transactions
                .Where(t =>
                    t.AccountId == accountId &&
                    t.Status == TransactionStatus.Posted &&
                    t.TransactionDate >= periodStart &&
                    t.TransactionDate < periodEnd)
                .OrderBy(t => t.TransactionDate)
                .ThenBy(t => t.Id)
                .ToListAsync();

            decimal balance = openingBalance;
            decimal totalDeposits = 0m;
            decimal totalWithdrawals = 0m;

            var statementTransactions = new List<BankStatementTransactionDto>();

            foreach (var transaction in transactions)
            {
                decimal signedAmount = TaxReportService.GetSignedAmount(transaction);
                balance += signedAmount;

                if (transaction.Type == TransactionType.Deposit)
                    totalDeposits += transaction.Amount;
                else if (transaction.Type == TransactionType.Withdrawal)
                    totalWithdrawals += transaction.Amount;

                statementTransactions.Add(
                    new BankStatementTransactionDto(
                        Date: transaction.TransactionDate.ToString("yyyy-MM-dd"),
                        Type: TaxReportService.GetTransactionTypeName(transaction.Type),
                        AmountSek: signedAmount,
                        BalanceAfterSek: balance
                    ));
            }

            var metadata = new BankStatementMetadataDto(
                StatementId: $"STMT-{periodStart:yyyyMMdd}-{accountId:D5}",
                PeriodStart: periodStart.ToString("yyyy-MM-dd"),
                PeriodEnd: periodEnd.AddDays(-1).ToString("yyyy-MM-dd")
            );

            var customer = new BankStatementCustomerDto(
                FullName: account.Customer.Name,
                PersonalId: account.Customer.PersonalId,
                Address: account.Customer.Address
            );

            var accountDto = new BankStatementAccountDto(
                AccountNumber: account.AccountNumber,
                AccountType: account.AccountType,
                InterestRatePct: account.InterestRate * 100m
            );

            var summary = new BankStatementSummaryDto(
                OpeningBalanceSek: openingBalance,
                ClosingBalanceSek: balance,
                TotalDepositsSek: totalDeposits,
                TotalWithdrawalsSek: totalWithdrawals,
                TransactionCount: statementTransactions.Count
            );

            return new BankStatementDto(
                Metadata: metadata,
                Customer: customer,
                Account: accountDto,
                Summary: summary,
                Transactions: statementTransactions
            );
        }
    }
}