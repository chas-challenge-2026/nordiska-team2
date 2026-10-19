using Microsoft.EntityFrameworkCore;
using NordiskaPortal.Api.Data;
using NordiskaPortal.Api.DTOs;
using NordiskaPortal.Api.Models;

namespace NordiskaPortal.Api.Services
{
    public class TaxReportService
    {
        private readonly BankContext _db;

        public TaxReportService(BankContext db)
        {
            _db = db;
        }

        public async Task<TaxReportDto?> BuildReportAsync(int accountId, int year)
        {
            var account = await _db.SavingsAccounts
                .Include(a => a.Customer)
                .FirstOrDefaultAsync(a => a.Id == accountId);

            if (account == null || account.Customer == null)
                return null;

            var startDate = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = startDate.AddYears(1);

            decimal startingBalance = await GetBalanceAsOfAsync(accountId, startDate);

            var transactions = await _db.Transactions
                .Where(t =>
                    t.AccountId == accountId &&
                    t.Status == TransactionStatus.Posted &&
                    t.TransactionDate >= startDate &&
                    t.TransactionDate < endDate)
                .OrderBy(t => t.TransactionDate)
                .ThenBy(t => t.Id)
                .ToListAsync();

            decimal balance = startingBalance;

            decimal totalDeposits = 0m;
            decimal totalWithdrawals = 0m;
            decimal totalInterest = 0m;
            decimal totalTax = 0m;

            var reportTransactions = new List<TaxReportTransactionDto>();

            foreach (var transaction in transactions)
            {
                decimal signedAmount = GetSignedAmount(transaction);
                balance += signedAmount;

                switch (transaction.Type)
                {
                    case TransactionType.Deposit:
                        totalDeposits += transaction.Amount;
                        break;
                    case TransactionType.Withdrawal:
                        totalWithdrawals += transaction.Amount;
                        break;
                    case TransactionType.Interest:
                        totalInterest += transaction.Amount;
                        break;
                    case TransactionType.Tax:
                        totalTax += transaction.Amount;
                        break;
                }

                reportTransactions.Add(
                    new TaxReportTransactionDto(
                        Id: $"TX-{transaction.Id}",
                        Date: transaction.TransactionDate.ToString("yyyy-MM-dd"),
                        Type: GetTransactionTypeName(transaction.Type),
                        Description: GetTransactionDescription(transaction),
                        AmountSek: signedAmount,
                        BalanceAfterSek: balance
                    ));
            }

            var metadata = new TaxReportMetadataDto(
                ReportId: $"TAX-{year}-{accountId:D5}",
                ReportType: "ANNUAL_TAX_REPORT",
                Year: year,
                PeriodStart: startDate.ToString("yyyy-MM-dd"),
                PeriodEnd: endDate.AddDays(-1).ToString("yyyy-MM-dd"),
                GenerationDate: DateTime.UtcNow.ToString("O")
            );

            var customer = new TaxReportCustomerDto(
                CustomerId: account.Customer.Id.ToString(),
                PersonalId: account.Customer.PersonalId,
                FullName: account.Customer.Name,
                Address: account.Customer.Address
            );

            var accountDto = new TaxReportAccountDto(
                AccountNumber: account.AccountNumber,
                AccountType: account.AccountType,
                InterestRatePct: account.InterestRate * 100m
            );

            var summary = new TaxReportSummaryDto(
                StartingBalanceSek: startingBalance,
                EndingBalanceSek: balance,
                TotalDepositsSek: totalDeposits,
                TotalWithdrawalsSek: totalWithdrawals,
                TotalInterestEarnedSek: totalInterest,
                TotalTaxWithheldSek: totalTax
            );

            return new TaxReportDto(
                Metadata: metadata,
                Customer: customer,
                Account: accountDto,
                Summary: summary,
                Transactions: reportTransactions
            );
        }

        /// <summary>
        /// Sums every posted transaction strictly before <paramref name="cutoffUtc"/>,
        /// giving the account's balance at that moment. Used as a year's
        /// starting balance, which is by definition the previous year's
        /// ending balance.
        /// </summary>
        internal async Task<decimal> GetBalanceAsOfAsync(int accountId, DateTime cutoffUtc)
        {
            var priorTransactions = await _db.Transactions
                .Where(t =>
                    t.AccountId == accountId &&
                    t.Status == TransactionStatus.Posted &&
                    t.TransactionDate < cutoffUtc)
                .ToListAsync();

            return priorTransactions.Sum(GetSignedAmount);
        }

        internal static decimal GetSignedAmount(Transaction transaction)
        {
            return transaction.Type switch
            {
                TransactionType.Deposit => transaction.Amount,
                TransactionType.Interest => transaction.Amount,
                TransactionType.Withdrawal => -transaction.Amount,
                TransactionType.Tax => -transaction.Amount,
                _ => 0m
            };
        }

        internal static string GetTransactionTypeName(TransactionType type)
        {
            return type switch
            {
                TransactionType.Deposit => "Deposit",
                TransactionType.Withdrawal => "Withdrawal",
                TransactionType.Interest => "Interest",
                TransactionType.Tax => "Tax",
                _ => char.ToUpper(type.ToString()[0]) + type.ToString().Substring(1).ToLowerInvariant()
            };
        }

        internal static string GetTransactionDescription(Transaction transaction)
        {
            if (!string.IsNullOrWhiteSpace(transaction.Description))
                return transaction.Description;

            return transaction.Type switch
            {
                TransactionType.Deposit => "Insättning",
                TransactionType.Withdrawal => "Uttag",
                TransactionType.Interest => "Ränta",
                TransactionType.Tax => "Skatt",
                _ => transaction.Type.ToString()
            };
        }

        public async Task ApplyYearEndInterestAsync(int accountId, int year)
        {
            bool alreadyApplied = await _db.Transactions.AnyAsync(t =>
                t.AccountId == accountId &&
                t.Type == TransactionType.Interest &&
                t.TransactionDate.Year == year);

            if (alreadyApplied)
                return;

            var account = await _db.SavingsAccounts.FindAsync(accountId);
            if (account == null)
                return;

            var yearStart = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            decimal startingBalance = await GetBalanceAsOfAsync(accountId, yearStart);

            decimal interest = Math.Round(startingBalance * account.InterestRate, 2);
            if (interest <= 0m)
                return;

            var interestDate = new DateTime(year, 12, 31, 0, 0, 0, DateTimeKind.Utc);

            _db.Transactions.Add(new Transaction
            {
                AccountId = accountId,
                Type = TransactionType.Interest,
                Description = $"Årsränta {year} ({account.InterestRate:P2})",
                Amount = interest,
                TransactionDate = interestDate,
                PostingDate = Transaction.CalculatePostingDate(interestDate),
                Status = TransactionStatus.Posted
            });

            await _db.SaveChangesAsync();
        }
    }
}