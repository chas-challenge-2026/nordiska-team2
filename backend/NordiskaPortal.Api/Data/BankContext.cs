using Microsoft.EntityFrameworkCore;
using NordiskaPortal.Api.Models;

namespace NordiskaPortal.Api.Data
{
    public class BankContext : DbContext
    {
        public DbSet<Customer> Customers { get; set; }
        public DbSet<SavingsAccount> SavingsAccounts { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .Build();

                var connectionString = configuration.GetConnectionString("DefaultConnection");
                optionsBuilder.UseNpgsql(connectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Customer>().HasIndex(c => c.Email).IsUnique();

            modelBuilder.Entity<Customer>().HasData(
                new Customer
                {
                    Id = 1,
                    Name = "Anna Lindqvist",
                    Email = "anna@example.com",
                    PasswordMd5 = "482c811da5d5b4bc6d497ffa98491e38",
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Customer
                {
                    Id = 2,
                    Name = "Erik Johansson",
                    Email = "erik@example.com",
                    PasswordMd5 = "482c811da5d5b4bc6d497ffa98491e38",
                    CreatedAt = new DateTime(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc)
                }
            );

            modelBuilder.Entity<SavingsAccount>().HasData(
                new SavingsAccount
                {
                    Id = 1,
                    CustomerId = 1,
                    AccountNumber = "NKM-10001",
                    Balance = 125000.00m,
                    InterestRate = 0.0350m,
                    AccountType = "Savings",
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new SavingsAccount
                {
                    Id = 2,
                    CustomerId = 1,
                    AccountNumber = "NKM-10002",
                    Balance = 45000.00m,
                    InterestRate = 0.0280m,
                    AccountType = "Savings",
                    CreatedAt = new DateTime(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc)
                },
                new SavingsAccount
                {
                    Id = 3,
                    CustomerId = 2,
                    AccountNumber = "NKM-20001",
                    Balance = 89500.00m,
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
                    Type = "Deposit",
                    Amount = 125000.00m,
                    BalanceAfter = 125000.00m,
                    TransactionDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    PostingDate = Transaction.CalculatePostingDate(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
                    Status = TransactionStatus.Posted
                },
                new Transaction
                {
                    Id = 2,
                    AccountId = 2,
                    Type = "Deposit",
                    Amount = 45000.00m,
                    BalanceAfter = 45000.00m,
                    TransactionDate = new DateTime(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc),
                    PostingDate = Transaction.CalculatePostingDate(new DateTime(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc)),
                    Status = TransactionStatus.Posted
                },
                new Transaction
                {
                    Id = 3,
                    AccountId = 3,
                    Type = "Withdrawal",
                    Amount = 89500.00m,
                    BalanceAfter = 89500.00m,
                    TransactionDate = new DateTime(2026, 3, 3, 0, 0, 0, DateTimeKind.Utc),
                    PostingDate = Transaction.CalculatePostingDate(new DateTime(2026, 3, 3, 0, 0, 0, DateTimeKind.Utc)),
                    Status = TransactionStatus.Posted
                }
            );
        }
    }
}