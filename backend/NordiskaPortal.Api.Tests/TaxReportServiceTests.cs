using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using NordiskaPortal.Api.Data;
using NordiskaPortal.Api.Models;
using NordiskaPortal.Api.Services;
using Xunit;

namespace NordiskaPortal.Api.Tests.Services
{
    public class TaxReportServiceTests
    {
        private static BankContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<BankContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new BankContext(options);
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

        // Verifies that building a report for a non-existent account returns null.
        [Fact]
        public async Task BuildReportAsync_ReturnsNull_WhenAccountDoesNotExist()
        {
            using var db = CreateContext(
                nameof(BuildReportAsync_ReturnsNull_WhenAccountDoesNotExist));

            var service = new TaxReportService(db);

            var result = await service.BuildReportAsync(
                accountId: 999,
                year: 2024);

            Assert.Null(result);
        }

        // Verifies that an account with no prior transactions has a starting balance of zero.
        [Fact]
        public async Task BuildReportAsync_StartingBalance_IsZero_WhenNoPriorTransactions()
        {
            using var db = CreateContext(
                nameof(BuildReportAsync_StartingBalance_IsZero_WhenNoPriorTransactions));

            await SeedBasicAccountAsync(db);

            var service = new TaxReportService(db);

            var result = await service.BuildReportAsync(
                accountId: 1,
                year: 2024);

            Assert.NotNull(result);
            Assert.Equal(
                0m,
                result!.Summary.StartingBalanceSek);
        }

        // Verifies that the starting balance includes prior posted transactions but excludes transactions from the report year.
        [Fact]
        public async Task BuildReportAsync_StartingBalance_EqualsSumOfPriorPostedTransactions()
        {
            using var db = CreateContext(
                nameof(BuildReportAsync_StartingBalance_EqualsSumOfPriorPostedTransactions));

            await SeedBasicAccountAsync(db);

            db.Transactions.AddRange(
                new Transaction
                {
                    Id = 1,
                    AccountId = 1,
                    Type = TransactionType.Deposit,
                    Amount = 10000m,
                    TransactionDate = new DateTime(
                        2023, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                    PostingDate = new DateTime(
                        2023, 3, 3, 0, 0, 0, DateTimeKind.Utc),
                    Status = TransactionStatus.Posted
                },
                new Transaction
                {
                    Id = 2,
                    AccountId = 1,
                    Type = TransactionType.Withdrawal,
                    Amount = 2000m,
                    TransactionDate = new DateTime(
                        2023, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                    PostingDate = new DateTime(
                        2023, 6, 3, 0, 0, 0, DateTimeKind.Utc),
                    Status = TransactionStatus.Posted
                },
                // This transaction happens in 2024, so it must NOT count
                // toward the 2024 starting balance.
                new Transaction
                {
                    Id = 3,
                    AccountId = 1,
                    Type = TransactionType.Deposit,
                    Amount = 500m,
                    TransactionDate = new DateTime(
                        2024, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                    PostingDate = new DateTime(
                        2024, 2, 3, 0, 0, 0, DateTimeKind.Utc),
                    Status = TransactionStatus.Posted
                }
            );

            await db.SaveChangesAsync();

            var service = new TaxReportService(db);

            var result = await service.BuildReportAsync(
                accountId: 1,
                year: 2024);

            Assert.NotNull(result);

            // 10000 deposit - 2000 withdrawal = 8000 SEK.
            Assert.Equal(
                8000m,
                result!.Summary.StartingBalanceSek);
        }

        // Verifies that the ending balance equals the starting balance plus the year's net change.
        [Fact]
        public async Task BuildReportAsync_EndingBalance_IsStartingBalancePlusYearsNetChange()
        {
            using var db = CreateContext(
                nameof(BuildReportAsync_EndingBalance_IsStartingBalancePlusYearsNetChange));

            await SeedBasicAccountAsync(db);

            db.Transactions.AddRange(
                new Transaction
                {
                    Id = 1,
                    AccountId = 1,
                    Type = TransactionType.Deposit,
                    Amount = 5000m,
                    TransactionDate = new DateTime(
                        2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    PostingDate = new DateTime(
                        2023, 1, 3, 0, 0, 0, DateTimeKind.Utc),
                    Status = TransactionStatus.Posted
                },
                new Transaction
                {
                    Id = 2,
                    AccountId = 1,
                    Type = TransactionType.Deposit,
                    Amount = 1000m,
                    TransactionDate = new DateTime(
                        2024, 5, 1, 0, 0, 0, DateTimeKind.Utc),
                    PostingDate = new DateTime(
                        2024, 5, 3, 0, 0, 0, DateTimeKind.Utc),
                    Status = TransactionStatus.Posted
                }
            );

            await db.SaveChangesAsync();

            var service = new TaxReportService(db);

            var result = await service.BuildReportAsync(
                accountId: 1,
                year: 2024);

            Assert.NotNull(result);

            // 2023 transaction establishes a starting balance of 5000 SEK.
            Assert.Equal(
                5000m,
                result!.Summary.StartingBalanceSek);

            // 5000 starting balance + 1000 received during 2024 = 6000 SEK.
            Assert.Equal(
                6000m,
                result!.Summary.EndingBalanceSek);
        }

        // Verifies that pending transactions are excluded from the tax report and do not affect the balance.
        [Fact]
        public async Task BuildReportAsync_ExcludesPendingTransactions()
        {
            using var db = CreateContext(
                nameof(BuildReportAsync_ExcludesPendingTransactions));

            await SeedBasicAccountAsync(db);

            db.Transactions.Add(new Transaction
            {
                Id = 1,
                AccountId = 1,
                Type = TransactionType.Deposit,
                Amount = 10000m,
                TransactionDate = new DateTime(
                    2024, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                PostingDate = new DateTime(
                    2024, 3, 3, 0, 0, 0, DateTimeKind.Utc),

                // Pending transactions should not be included in the report.
                Status = TransactionStatus.Pending
            });

            await db.SaveChangesAsync();

            var service = new TaxReportService(db);

            var result = await service.BuildReportAsync(
                accountId: 1,
                year: 2024);

            Assert.NotNull(result);

            // The pending transaction should not appear in the report.
            Assert.Empty(result!.Transactions);

            // Because the transaction is pending, it should not affect the balance.
            Assert.Equal(
                0m,
                result!.Summary.EndingBalanceSek);
        }

        // Verifies that the balance calculation correctly sums posted transactions up to the specified cutoff date.
        [Fact]
        public async Task GetBalanceAsOfAsync_MatchesManualSum()
        {
            using var db = CreateContext(
                nameof(GetBalanceAsOfAsync_MatchesManualSum));

            await SeedBasicAccountAsync(db);

            db.Transactions.AddRange(
                new Transaction
                {
                    Id = 1,
                    AccountId = 1,
                    Type = TransactionType.Deposit,
                    Amount = 100m,
                    TransactionDate = new DateTime(
                        2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    PostingDate = new DateTime(
                        2022, 1, 3, 0, 0, 0, DateTimeKind.Utc),
                    Status = TransactionStatus.Posted
                },
                new Transaction
                {
                    Id = 2,
                    AccountId = 1,
                    Type = TransactionType.Interest,
                    Amount = 5m,
                    TransactionDate = new DateTime(
                        2022, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                    PostingDate = new DateTime(
                        2022, 6, 3, 0, 0, 0, DateTimeKind.Utc),
                    Status = TransactionStatus.Posted
                },
                new Transaction
                {
                    Id = 3,
                    AccountId = 1,
                    Type = TransactionType.Tax,
                    Amount = 2m,
                    TransactionDate = new DateTime(
                        2022, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                    PostingDate = new DateTime(
                        2023, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                    Status = TransactionStatus.Posted
                }
            );

            await db.SaveChangesAsync();

            var service = new TaxReportService(db);

            var cutoff = new DateTime(
                2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            decimal balance = await service.GetBalanceAsOfAsync(
                accountId: 1,
                cutoffUtc: cutoff);

            // 100 deposit + 5 interest - 2 tax = 103 SEK.
            Assert.Equal(103m, balance);
        }
    }
}
