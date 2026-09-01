using Microsoft.EntityFrameworkCore;
using NordiskaPortal.Api.Models;

namespace NordiskaPortal.Api.Data
{
    public class BankContext : DbContext
    {
        public DbSet<Customer> Customers { get; set; }
        public DbSet<SavingsAccount> SavingsAccounts { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        public BankContext(DbContextOptions<BankContext> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                /*
                    Design-time fallback (e.g. `dotnet ef migrations add`).

                    Since EF's design-time factory calls this constructor path 
                    before Program.cs's DI-based configuration runs. 
                    
                    Includes appsettings.Development.json so `dotnet ef database 
                    update` can actually reach a real database, not just generate 
                    migration files against an empty connection string.
                */
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .AddJsonFile("appsettings.Development.json", optional: true)
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
                    PasswordHash = "$2b$12$hg6bJTmUyy.QTahIR9LWf.6vdXcGceKXaMd0r4mOeVbyvAAeX8vEO",
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Customer
                {
                    Id = 2,
                    Name = "Erik Johansson",
                    Email = "erik@example.com",
                    PasswordHash = "$2b$12$hg6bJTmUyy.QTahIR9LWf.6vdXcGceKXaMd0r4mOeVbyvAAeX8vEO",
                    CreatedAt = new DateTime(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc)
                }
            );

            modelBuilder.Entity<SavingsAccount>().HasData(

                /*
                    Removed Balance seed value.
                    
                    Balance is now always derived from Transactions (SUM over Posted rows). 
                    The opening balance for this account comes from the seeded Transaction 
                    below instead.
                */
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
                /*
                    Removed BalanceAfter.

                    Balance is now just SUM(Amount) over its Posted transactions.
                */
                new Transaction
                {
                    Id = 1,
                    AccountId = 1,
                    Type = "Deposit",
                    Amount = 125000.00m,
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
                    TransactionDate = new DateTime(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc),
                    PostingDate = Transaction.CalculatePostingDate(new DateTime(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc)),
                    Status = TransactionStatus.Posted
                },
                new Transaction
                {
                    Id = 3,
                    AccountId = 3,
                    Type = "Deposit",
                    Amount = 89500.00m,
                    TransactionDate = new DateTime(2026, 3, 3, 0, 0, 0, DateTimeKind.Utc),
                    PostingDate = Transaction.CalculatePostingDate(new DateTime(2026, 3, 3, 0, 0, 0, DateTimeKind.Utc)),
                    Status = TransactionStatus.Posted
                }
            );
        }
    }
}