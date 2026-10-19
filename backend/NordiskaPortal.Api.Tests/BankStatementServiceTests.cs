using Microsoft.EntityFrameworkCore;
using NordiskaPortal.Api.Data;
using NordiskaPortal.Api.Models;
using NordiskaPortal.Api.Services;
using NordiskaPortal.Api.DTOs;
using Xunit;

namespace NordiskaPortal.Api.Tests.Services
{
    public class BankStatementServiceTests
    {
        private static BankContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<BankContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new BankContext(options);
        }

        private static (BankStatementService statement, TaxReportService taxReport) CreateServices(BankContext db)
        {
            var taxReportService = new TaxReportService(db);
            var statementService = new BankStatementService(db, taxReportService);
            return (statementService, taxReportService);
        }

        private static async Task SeedBasicAccountAsync(
            BankContext db,
            int customerId = 1,
            int accountId = 1)
        {
            db.Customers.Add(new Customer
            {
                Id = customerId,
                Name = "Test Customer",
                PersonalId = "19900101-1234",
                Address = "Test Street 1",
                Email = $"test{customerId}@example.com",
                PasswordHash = "hash",
                CreatedAt = DateTime.UtcNow
            });

            db.SavingsAccounts.Add(new SavingsAccount
            {
                Id = accountId,
                CustomerId = customerId,
                AccountNumber = "NKM-TEST",
                InterestRate = 0.035m,
                AccountType = "Savings"
            });

            await db.SaveChangesAsync();
        }

        // Verifies that building a statement for a non-existent account
        // returns null.
        [Fact]
        public async Task BuildStatementForMonthAsync_ReturnsNull_WhenAccountDoesNotExist()
        {
            using var db = CreateContext(
                nameof(BuildStatementForMonthAsync_ReturnsNull_WhenAccountDoesNotExist));

            var (statementService, _) = CreateServices(db);

            var result = await statementService.BuildStatementForMonthAsync(
                accountId: 999,
                year: 2026,
                month: 10);

            Assert.Null(result);
        }

        // Verifies that month must be between 1 and 12.
        [Theory]
        [InlineData(0)]
        [InlineData(13)]
        public async Task BuildStatementForMonthAsync_Throws_ForInvalidMonth(int invalidMonth)
        {
            using var db = CreateContext(
                nameof(BuildStatementForMonthAsync_Throws_ForInvalidMonth) + invalidMonth);

            await SeedBasicAccountAsync(db);

            var (statementService, _) = CreateServices(db);

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                statementService.BuildStatementForMonthAsync(accountId: 1, year: 2026, month: invalidMonth));
        }

        // Verifies that an account with no transactions before the period
        // has an opening balance of zero.
        [Fact]
        public async Task BuildStatementForMonthAsync_OpeningBalance_IsZero_WhenNoPriorTransactions()
        {
            using var db = CreateContext(
                nameof(BuildStatementForMonthAsync_OpeningBalance_IsZero_WhenNoPriorTransactions));

            await SeedBasicAccountAsync(db);

            var (statementService, _) = CreateServices(db);

            var result = await statementService.BuildStatementForMonthAsync(
                accountId: 1,
                year: 2026,
                month: 10);

            Assert.NotNull(result);
            Assert.Equal(0m, result!.Summary.OpeningBalanceSek);
        }

        // Verifies that the opening balance includes transactions from
        // before the period but excludes ones inside or after it.
        [Fact]
        public async Task BuildStatementForMonthAsync_OpeningBalance_ExcludesTransactionsInOrAfterPeriod()
        {
            using var db = CreateContext(
                nameof(BuildStatementForMonthAsync_OpeningBalance_ExcludesTransactionsInOrAfterPeriod));

            await SeedBasicAccountAsync(db);

            db.Transactions.AddRange(
                // Before the period: counts toward opening balance.
                new Transaction
                {
                    Id = 1,
                    AccountId = 1,
                    Type = TransactionType.Deposit,
                    Amount = 10000m,
                    TransactionDate = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc),
                    PostingDate = new DateTime(2026, 9, 17, 0, 0, 0, DateTimeKind.Utc),
                    Status = TransactionStatus.Posted
                },
                // Inside the period: must NOT count toward opening balance.
                new Transaction
                {
                    Id = 2,
                    AccountId = 1,
                    Type = TransactionType.Deposit,
                    Amount = 500m,
                    TransactionDate = new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc),
                    PostingDate = new DateTime(2026, 10, 7, 0, 0, 0, DateTimeKind.Utc),
                    Status = TransactionStatus.Posted
                },
                // After the period: must NOT count toward opening balance.
                new Transaction
                {
                    Id = 3,
                    AccountId = 1,
                    Type = TransactionType.Deposit,
                    Amount = 999m,
                    TransactionDate = new DateTime(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc),
                    PostingDate = new DateTime(2026, 11, 4, 0, 0, 0, DateTimeKind.Utc),
                    Status = TransactionStatus.Posted
                }
            );
            await db.SaveChangesAsync();

            var (statementService, _) = CreateServices(db);

            var result = await statementService.BuildStatementForMonthAsync(
                accountId: 1,
                year: 2026,
                month: 10);

            Assert.NotNull(result);
            Assert.Equal(10000m, result!.Summary.OpeningBalanceSek);
        }

        // Verifies that the closing balance equals the opening balance plus
        // the period's net change, and that only in-period transactions
        // appear in the transaction list.
        [Fact]
        public async Task BuildStatementForMonthAsync_ClosingBalance_IsOpeningPlusPeriodNetChange()
        {
            using var db = CreateContext(
                nameof(BuildStatementForMonthAsync_ClosingBalance_IsOpeningPlusPeriodNetChange));

            await SeedBasicAccountAsync(db);

            db.Transactions.AddRange(
                new Transaction
                {
                    Id = 1,
                    AccountId = 1,
                    Type = TransactionType.Deposit,
                    Amount = 5000m,
                    TransactionDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                    PostingDate = new DateTime(2026, 9, 3, 0, 0, 0, DateTimeKind.Utc),
                    Status = TransactionStatus.Posted
                },
                new Transaction
                {
                    Id = 2,
                    AccountId = 1,
                    Type = TransactionType.Deposit,
                    Amount = 1000m,
                    TransactionDate = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc),
                    PostingDate = new DateTime(2026, 10, 12, 0, 0, 0, DateTimeKind.Utc),
                    Status = TransactionStatus.Posted
                },
                new Transaction
                {
                    Id = 3,
                    AccountId = 1,
                    Type = TransactionType.Withdrawal,
                    Amount = 200m,
                    TransactionDate = new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc),
                    PostingDate = new DateTime(2026, 10, 22, 0, 0, 0, DateTimeKind.Utc),
                    Status = TransactionStatus.Posted
                }
            );
            await db.SaveChangesAsync();

            var (statementService, _) = CreateServices(db);

            var result = await statementService.BuildStatementForMonthAsync(
                accountId: 1,
                year: 2026,
                month: 10);

            Assert.NotNull(result);
            Assert.Equal(5000m, result!.Summary.OpeningBalanceSek);
            Assert.Equal(5800m, result!.Summary.ClosingBalanceSek); // 5000 + 1000 - 200
            Assert.Equal(1000m, result!.Summary.TotalDepositsSek);
            Assert.Equal(200m, result!.Summary.TotalWithdrawalsSek);
            Assert.Equal(2, result!.Transactions.Count);
        }

        // Verifies that pending transactions are excluded from both the
        // balance and the transaction list.
        [Fact]
        public async Task BuildStatementForMonthAsync_ExcludesPendingTransactions()
        {
            using var db = CreateContext(
                nameof(BuildStatementForMonthAsync_ExcludesPendingTransactions));

            await SeedBasicAccountAsync(db);

            db.Transactions.Add(new Transaction
            {
                Id = 1,
                AccountId = 1,
                Type = TransactionType.Deposit,
                Amount = 10000m,
                TransactionDate = new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc),
                PostingDate = new DateTime(2026, 10, 7, 0, 0, 0, DateTimeKind.Utc),
                Status = TransactionStatus.Pending
            });
            await db.SaveChangesAsync();

            var (statementService, _) = CreateServices(db);

            var result = await statementService.BuildStatementForMonthAsync(
                accountId: 1,
                year: 2026,
                month: 10);

            Assert.NotNull(result);
            Assert.Empty(result!.Transactions);
            Assert.Equal(0m, result!.Summary.ClosingBalanceSek);
        }

        // Verifies that the statement's period_start and period_end cover
        // exactly the calendar month requested.
        [Fact]
        public async Task BuildStatementForMonthAsync_PeriodCoversFullCalendarMonth()
        {
            using var db = CreateContext(
                nameof(BuildStatementForMonthAsync_PeriodCoversFullCalendarMonth));

            await SeedBasicAccountAsync(db);

            var (statementService, _) = CreateServices(db);

            var result = await statementService.BuildStatementForMonthAsync(
                accountId: 1,
                year: 2026,
                month: 2); // February - good for catching off-by-one day bugs

            Assert.NotNull(result);
            Assert.Equal("2026-02-01", result!.Metadata.PeriodStart);
            Assert.Equal("2026-02-28", result!.Metadata.PeriodEnd);
        }

        // Verifies that transaction_count in the summary matches the number
        // of transactions actually listed.
        [Fact]
        public async Task BuildStatementForMonthAsync_TransactionCount_MatchesListedTransactions()
        {
            using var db = CreateContext(
                nameof(BuildStatementForMonthAsync_TransactionCount_MatchesListedTransactions));

            await SeedBasicAccountAsync(db);

            db.Transactions.AddRange(
                new Transaction { Id = 1, AccountId = 1, Type = TransactionType.Deposit, Amount = 100m, TransactionDate = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc), PostingDate = new DateTime(2026, 10, 3, 0, 0, 0, DateTimeKind.Utc), Status = TransactionStatus.Posted },
                new Transaction { Id = 2, AccountId = 1, Type = TransactionType.Deposit, Amount = 200m, TransactionDate = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc), PostingDate = new DateTime(2026, 10, 17, 0, 0, 0, DateTimeKind.Utc), Status = TransactionStatus.Posted },
                new Transaction { Id = 3, AccountId = 1, Type = TransactionType.Withdrawal, Amount = 50m, TransactionDate = new DateTime(2026, 10, 28, 0, 0, 0, DateTimeKind.Utc), PostingDate = new DateTime(2026, 10, 30, 0, 0, 0, DateTimeKind.Utc), Status = TransactionStatus.Posted }
            );
            await db.SaveChangesAsync();

            var (statementService, _) = CreateServices(db);

            var result = await statementService.BuildStatementForMonthAsync(
                accountId: 1,
                year: 2026,
                month: 10);

            Assert.NotNull(result);
            Assert.Equal(3, result!.Summary.TransactionCount);
            Assert.Equal(result.Summary.TransactionCount, result.Transactions.Count);
        }

        // Verifies that the statement id is stable and derived from the
        // period and account, matching what the native engine's filename
        // extraction expects.
        [Fact]
        public async Task BuildStatementForMonthAsync_GeneratesExpectedStatementId()
        {
            using var db = CreateContext(
                nameof(BuildStatementForMonthAsync_GeneratesExpectedStatementId));

            await SeedBasicAccountAsync(db, customerId: 1, accountId: 7);

            var (statementService, _) = CreateServices(db);

            var result = await statementService.BuildStatementForMonthAsync(
                accountId: 7,
                year: 2026,
                month: 3);

            Assert.NotNull(result);
            Assert.Equal("STMT-20260301-00007", result!.Metadata.StatementId);
        }

        // Verifies, against the real seed data, that building a statement
        // for an existing account and a past month succeeds end to end.
        [Fact]
        public async Task SeedData_BuildsStatement_ForExistingAccount()
        {
            using var db = CreateContext(
                nameof(SeedData_BuildsStatement_ForExistingAccount));

            db.Database.EnsureCreated();

            var (statementService, _) = CreateServices(db);

            var result = await statementService.BuildStatementForMonthAsync(
                accountId: 1,
                year: 2023,
                month: 1);

            Assert.NotNull(result);
            Assert.Equal("NKM-10001", result!.Account.AccountNumber);
        }
    }
}