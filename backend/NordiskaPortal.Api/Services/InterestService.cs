using Microsoft.EntityFrameworkCore;
using NordiskaPortal.Api.Data;
using NordiskaPortal.Api.DTOs;
using NordiskaPortal.Api.Models;

namespace NordiskaPortal.Api.Services
{
    public class InterestService
    {
        private readonly BankContext _db;

        public InterestService(BankContext db)
        {
            _db = db;
        }

        // Interest for the current year: what has accrued so far, and what
        // the year-end payout will be if nothing else happens.
        // asOfUtc defaults to now; tests pass a fixed date.
        public async Task<InterestSummaryResponse?> GetInterestSummaryAsync(int accountId, DateTime? asOfUtc = null)
        {
            var asOf = asOfUtc ?? DateTime.UtcNow;

            var account = await _db.SavingsAccounts.FindAsync(accountId);
            if (account == null)
                return null;

            var calculation = await CalculateAsync(account, asOf.Year, asOf);

            decimal paidTotal = await _db.Transactions
                .Where(t =>
                    t.AccountId == accountId &&
                    t.Status == TransactionStatus.Posted &&
                    t.Type == TransactionType.Interest &&
                    t.TransactionDate <= asOf)
                .SumAsync(t => t.Amount);

            return new InterestSummaryResponse(
                Year: asOf.Year,
                InterestRatePct: account.InterestRate * 100m,
                EarnedToDate: Math.Round(calculation.EarnedToDate, 2),
                ProjectedAtYearEnd: Math.Round(calculation.ProjectedForYear, 2),
                InterestPaidTotal: paidTotal,
                PaymentDate: new DateOnly(asOf.Year, 12, 31));
        }

        // Pays out a full year's interest as one Interest transaction on 31 December.
        // Safe to call twice: a year that already has an Interest row is skipped.
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

            var interestDate = new DateTime(year, 12, 31, 0, 0, 0, DateTimeKind.Utc);

            // Measured to the last day of the year, so "earned to date" is the whole year.
            var calculation = await CalculateAsync(account, year, interestDate);

            decimal interest = Math.Round(calculation.EarnedToDate, 2);
            if (interest <= 0m)
                return;

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

        private sealed record InterestCalculation(decimal EarnedToDate, decimal ProjectedForYear);

        // Interest = sum over days of (closing balance that day x rate / days in year).
        // A transaction counts from the day it is made, so a deposit on day 10
        // earns interest for day 10 as well.
        private async Task<InterestCalculation> CalculateAsync(SavingsAccount account, int year, DateTime asOfUtc)
        {
            var yearStart = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var yearEnd = yearStart.AddYears(1);
            int daysInYear = (yearEnd - yearStart).Days;

            // First day that is NOT yet earned. "Today" counts, so it is tomorrow's date.
            var earnedUntil = DateTime.SpecifyKind(asOfUtc.Date, DateTimeKind.Utc).AddDays(1);
            if (earnedUntil > yearEnd) earnedUntil = yearEnd;
            if (earnedUntil < yearStart) earnedUntil = yearStart;

            // Posted transactions up to the cutoff. Interest paid during this year
            // is left out so it is not compounded within the year it was paid, which
            // also keeps the result the same before and after the payout.
            var transactions = await _db.Transactions
                .Where(t =>
                    t.AccountId == account.Id &&
                    t.Status == TransactionStatus.Posted &&
                    t.TransactionDate < earnedUntil &&
                    !(t.Type == TransactionType.Interest && t.TransactionDate >= yearStart))
                .OrderBy(t => t.TransactionDate)
                .ThenBy(t => t.Id)
                .ToListAsync();

            decimal balance = transactions
                .Where(t => t.TransactionDate < yearStart)
                .Sum(SignedAmount);

            decimal balanceDays = 0m;
            var cursor = yearStart;

            foreach (var transaction in transactions.Where(t => t.TransactionDate >= yearStart))
            {
                var day = transaction.TransactionDate.Date;

                balanceDays += balance * (day - cursor).Days;
                balance += SignedAmount(transaction);
                cursor = day;
            }

            balanceDays += balance * (earnedUntil - cursor).Days;

            decimal earned = balanceDays * account.InterestRate / daysInYear;

            // Projection: the current balance stays for the rest of the year.
            decimal projectedBalanceDays = balanceDays + balance * (yearEnd - earnedUntil).Days;
            decimal projected = projectedBalanceDays * account.InterestRate / daysInYear;

            return new InterestCalculation(earned, projected);
        }

        // Which transaction types add money and which remove it.
        private static decimal SignedAmount(Transaction transaction)
        {
            return transaction.Type switch
            {
                TransactionType.Deposit => transaction.Amount,
                TransactionType.Interest => transaction.Amount,
                TransactionType.TransferIn => transaction.Amount,
                TransactionType.Withdrawal => -transaction.Amount,
                TransactionType.Tax => -transaction.Amount,
                TransactionType.TransferOut => -transaction.Amount,
                _ => 0m
            };
        }
    }
}