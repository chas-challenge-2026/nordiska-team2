using Microsoft.EntityFrameworkCore;
using NordiskaPortal.Api.Models;

namespace NordiskaPortal.Api.Data;

public class BankContext : DbContext
{
    public BankContext(DbContextOptions<BankContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers { get; set; }
    public DbSet<SavingsAccount> SavingsAccounts { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<TaxReport> TaxReports { get; set; }
    public DbSet<SavingsGoal> SavingsGoals { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Transaction>()
            .Property(t => t.Type)
            .HasConversion<string>();

        modelBuilder.Entity<Customer>().HasIndex(c => c.Email).IsUnique();

        modelBuilder.Entity<TaxReport>().HasIndex(r => new { r.AccountId, r.Year }).IsUnique();

        modelBuilder.Entity<Customer>().HasData(
            new Customer
            {
                Id = 1,
                Name = "Anna Lindqvist",
                PersonalId = "19850505-1234",
                Address = "Storgatan 1, 111 22 Stockholm",
                Email = "anna@example.com",
                PasswordHash = "$2b$12$hg6bJTmUyy.QTahIR9LWf.6vdXcGceKXaMd0r4mOeVbyvAAeX8vEO",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Customer
            {
                Id = 2,
                Name = "Erik Johansson",
                PersonalId = "19991212-5678",
                Address = "Kungsgatan 5, 411 19 Göteborg",
                Email = "erik@example.com",
                PasswordHash = "$2b$12$hg6bJTmUyy.QTahIR9LWf.6vdXcGceKXaMd0r4mOeVbyvAAeX8vEO",
                CreatedAt = new DateTime(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        modelBuilder.Entity<SavingsAccount>().HasData(
            new SavingsAccount
            {
                Id = 1,
                CustomerId = 1,
                AccountNumber = "NKM-10001",
                InterestRate = 0.0350m,
                AccountType = "Savings",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new SavingsAccount
            {
                Id = 2,
                CustomerId = 1,
                AccountNumber = "NKM-10002",
                InterestRate = 0.0280m,
                AccountType = "Savings",
                CreatedAt = new DateTime(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc)
            },
            new SavingsAccount
            {
                Id = 3,
                CustomerId = 2,
                AccountNumber = "NKM-20001",
                InterestRate = 0.0350m,
                AccountType = "Savings",
                CreatedAt = new DateTime(2026, 3, 3, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        modelBuilder.Entity<Transaction>().HasData(
            new Transaction
            {
                Id = 1,
                AccountId = 1,
                Type = TransactionType.Deposit,
                Description = "Lön",
                Amount = 125000.00m,
                TransactionDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                PostingDate = Transaction.CalculatePostingDate(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
                Status = TransactionStatus.Posted
            },
            new Transaction
            {
                Id = 2,
                AccountId = 2,
                Type = TransactionType.Deposit,
                Description = "Swish",
                Amount = 45000.00m,
                TransactionDate = new DateTime(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc),
                PostingDate = Transaction.CalculatePostingDate(new DateTime(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc)),
                Status = TransactionStatus.Posted
            },
            new Transaction
            {
                Id = 3,
                AccountId = 3,
                Type = TransactionType.Deposit,
                Description = "Lön",
                Amount = 89500.00m,
                TransactionDate = new DateTime(2026, 3, 3, 0, 0, 0, DateTimeKind.Utc),
                PostingDate = Transaction.CalculatePostingDate(new DateTime(2026, 3, 3, 0, 0, 0, DateTimeKind.Utc)),
                Status = TransactionStatus.Posted
            },
            new Transaction
            {
                Id = 4,
                AccountId = 1,
                Type = TransactionType.Deposit,
                Description = "Swish",
                Amount = 89500.00m,
                TransactionDate = new DateTime(2025, 3, 3, 0, 0, 0, DateTimeKind.Utc),
                PostingDate = Transaction.CalculatePostingDate(new DateTime(2026, 3, 3, 0, 0, 0, DateTimeKind.Utc)),
                Status = TransactionStatus.Posted
            },
            new Transaction
            {
                Id = 5,
                AccountId = 1,
                Type = TransactionType.Deposit,
                Description = "Lön",
                Amount = 40500.00m,
                TransactionDate = new DateTime(2025, 4, 4, 0, 0, 0, DateTimeKind.Utc),
                PostingDate = Transaction.CalculatePostingDate(new DateTime(2026, 3, 3, 0, 0, 0, DateTimeKind.Utc)),
                Status = TransactionStatus.Posted
            },
            new Transaction
            {
                Id = 6,
                AccountId = 1,
                Type = TransactionType.Withdrawal,
                Description = "Semester",
                Amount = 20000.00m,
                TransactionDate = new DateTime(2025, 5, 5, 0, 0, 0, DateTimeKind.Utc),
                PostingDate = Transaction.CalculatePostingDate(new DateTime(2026, 3, 3, 0, 0, 0, DateTimeKind.Utc)),
                Status = TransactionStatus.Posted
            },
            new Transaction
            {
                Id = 7,
                AccountId = 1,
                Type = TransactionType.Deposit,
                Description = "Lön",
                Amount = 50000.00m,
                TransactionDate = new DateTime(2023, 1, 15, 0, 0, 0, DateTimeKind.Utc),
                PostingDate = Transaction.CalculatePostingDate(new DateTime(2023, 1, 15, 0, 0, 0, DateTimeKind.Utc)),
                Status = TransactionStatus.Posted
            }
        );
    }
}