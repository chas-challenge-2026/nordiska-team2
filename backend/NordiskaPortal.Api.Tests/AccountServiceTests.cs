using Microsoft.EntityFrameworkCore;
using NordiskaPortal.Api.Data;
using NordiskaPortal.Api.Models;
using NordiskaPortal.Api.Services;

namespace NordiskaPortal.Api.Tests;

public class AccountServiceTests
{
    private static BankContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<BankContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new BankContext(options);
    }

    private static async Task SeedTestData(BankContext db)
    {
        db.Customers.Add(new Customer
        {
            Id = 1,
            Name = "Test Testsson",
            PersonalId = "19900101-1234",
            Address = "Testgatan 1",
            Email = "test@test.com",
            PasswordHash = "hash"
        });

        db.SavingsAccounts.AddRange(
            new SavingsAccount { Id = 1, CustomerId = 1, AccountNumber = "TEST-001" },
            new SavingsAccount { Id = 2, CustomerId = 1, AccountNumber = "TEST-002" }
        );

        // Kund 2 — ska INTE synas i kund 1:s summary
        db.Customers.Add(new Customer
        {
            Id = 2,
            Name = "Annan Person",
            PersonalId = "19950606-5678",
            Address = "Annanvägen 2",
            Email = "annan@test.com",
            PasswordHash = "hash"
        });

        db.SavingsAccounts.Add(
            new SavingsAccount { Id = 3, CustomerId = 2, AccountNumber = "OTHER-001" }
        );

        db.Transactions.AddRange(
            // September 2026 — kund 1, konto 1
            new Transaction
            {
                Id = 1, AccountId = 1,
                Type = TransactionType.Deposit, Amount = 10000m,
                TransactionDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                PostingDate = new DateTime(2026, 9, 3, 0, 0, 0, DateTimeKind.Utc),
                Status = TransactionStatus.Posted
            },
            new Transaction
            {
                Id = 2, AccountId = 1,
                Type = TransactionType.Withdrawal, Amount = 3000m,
                TransactionDate = new DateTime(2026, 9, 5, 0, 0, 0, DateTimeKind.Utc),
                PostingDate = new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc),
                Status = TransactionStatus.Posted
            },
            // September 2026 — kund 1, konto 2 (testar att det räknar tvärs konton)
            new Transaction
            {
                Id = 3, AccountId = 2,
                Type = TransactionType.Interest, Amount = 500m,
                TransactionDate = new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc),
                PostingDate = new DateTime(2026, 9, 12, 0, 0, 0, DateTimeKind.Utc),
                Status = TransactionStatus.Posted
            },
            // Augusti 2026 — ska INTE räknas (utanför perioden)
            new Transaction
            {
                Id = 4, AccountId = 1,
                Type = TransactionType.Deposit, Amount = 99999m,
                TransactionDate = new DateTime(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc),
                PostingDate = new DateTime(2026, 8, 17, 0, 0, 0, DateTimeKind.Utc),
                Status = TransactionStatus.Posted
            },
            // September 2026 — kund 2 (ska INTE synas för kund 1)
            new Transaction
            {
                Id = 5, AccountId = 3,
                Type = TransactionType.Deposit, Amount = 77777m,
                TransactionDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                PostingDate = new DateTime(2026, 9, 3, 0, 0, 0, DateTimeKind.Utc),
                Status = TransactionStatus.Posted
            },
            // September 2026 — kund 1, men Pending (ska INTE räknas)
            new Transaction
            {
                Id = 6, AccountId = 1,
                Type = TransactionType.Deposit, Amount = 5000m,
                TransactionDate = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc),
                PostingDate = new DateTime(2026, 9, 17, 0, 0, 0, DateTimeKind.Utc),
                Status = TransactionStatus.Pending
            }
        );

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetFinancialSummary_ReturnsCorrectIncomeAndExpenses()
    {
        // Arrange
        using var db = CreateInMemoryDb();
        await SeedTestData(db);
        var transactionService = new TransactionService(db);
        var service = new AccountService(db, transactionService);

        var from = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 9, 30, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var result = await service.GetFinancialSummaryAsync(1, from, to);

        // Assert
        Assert.Equal(10500m, result.Income);    // 10000 (Deposit) + 500 (Interest)
        Assert.Equal(3000m, result.Expenses);   // 3000 (Withdrawal)
    }

    [Fact]
    public async Task GetFinancialSummary_ExcludesTransactionsOutsidePeriod()
    {
        // Arrange
        using var db = CreateInMemoryDb();
        await SeedTestData(db);
        var transactionService = new TransactionService(db);
        var service = new AccountService(db, transactionService);

        // Augusti 2026 — ska bara inkludera transaktion #4 (99999 kr)
        var from = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 8, 31, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var result = await service.GetFinancialSummaryAsync(1, from, to);

        // Assert
        Assert.Equal(99999m, result.Income);
        Assert.Equal(0m, result.Expenses);
    }

    [Fact]
    public async Task GetFinancialSummary_DoesNotIncludeOtherCustomersTransactions()
    {
        // Arrange
        using var db = CreateInMemoryDb();
        await SeedTestData(db);
        var transactionService = new TransactionService(db);
        var service = new AccountService(db, transactionService);

        var from = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 9, 30, 23, 59, 59, DateTimeKind.Utc);

        // Act — kund 2
        var result = await service.GetFinancialSummaryAsync(2, from, to);

        // Assert — bara kund 2:s 77777 kr, inte kund 1:s transaktioner
        Assert.Equal(77777m, result.Income);
        Assert.Equal(0m, result.Expenses);
    }

    [Fact]
    public async Task GetFinancialSummary_ExcludesPendingTransactions()
    {
        // Arrange
        using var db = CreateInMemoryDb();
        await SeedTestData(db);
        var transactionService = new TransactionService(db);
        var service = new AccountService(db, transactionService);

        var from = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 9, 30, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var result = await service.GetFinancialSummaryAsync(1, from, to);

        // Assert — den Pending-insättningen på 5000 ska INTE räknas
        Assert.Equal(10500m, result.Income);  // bara 10000 + 500, inte 15500
    }

    [Fact]
    public async Task GetFinancialSummary_ReturnsZerosForEmptyPeriod()
    {
        // Arrange
        using var db = CreateInMemoryDb();
        await SeedTestData(db);
        var transactionService = new TransactionService(db);
        var service = new AccountService(db, transactionService);

        // Juli 2026 — inga transaktioner alls
        var from = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 7, 31, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var result = await service.GetFinancialSummaryAsync(1, from, to);

        // Assert
        Assert.Equal(0m, result.Income);
        Assert.Equal(0m, result.Expenses);
    }

    [Fact]
    public async Task GetFinancialSummary_ReturnsPeriodDates()
    {
        // Arrange
        using var db = CreateInMemoryDb();
        await SeedTestData(db);
        var transactionService = new TransactionService(db);
        var service = new AccountService(db, transactionService);

        var from = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 9, 30, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var result = await service.GetFinancialSummaryAsync(1, from, to);

        // Assert — kontrollera att perioden returneras korrekt
        Assert.Equal(from, result.PeriodStart);
        Assert.Equal(to, result.PeriodEnd);
    }
}
