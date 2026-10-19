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
    public DbSet<FaqEntry> FaqEntries { get; set; }
    public DbSet<SavingsGoal> SavingsGoals { get; set; }
    public DbSet<AuditEntry> AuditEntries { get; set; }
    public DbSet<FaqEntryTranslation> FaqEntryTranslations { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Transaction>()
            .Property(t => t.Type)
            .HasConversion<string>();

        modelBuilder.Entity<Customer>().HasIndex(c => c.Email).IsUnique();

        modelBuilder.Entity<TaxReport>().HasIndex(r => new { r.AccountId, r.Year }).IsUnique();

        modelBuilder.Entity<FaqEntryTranslation>().HasIndex(t => new { t.FaqEntryId, t.Language }).IsUnique();

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

        modelBuilder.Entity<SavingsAccount>()
            .HasIndex(a => a.AccountNumber)
            .IsUnique();

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
                PostingDate = Transaction.CalculatePostingDate(new DateTime(2025, 3, 3, 0, 0, 0, DateTimeKind.Utc)),
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
                PostingDate = Transaction.CalculatePostingDate(new DateTime(2025, 4, 4, 0, 0, 0, DateTimeKind.Utc)),
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
                PostingDate = Transaction.CalculatePostingDate(new DateTime(2025, 5, 5, 0, 0, 0, DateTimeKind.Utc)),
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

        modelBuilder.Entity<FaqEntry>().HasData(
            // ===== Ränta: 4 entries (ids 1-4) =====
            new FaqEntry
            {
                Id = 1,
                Question = "När betalas räntan ut?",
                Answer = "Räntan beräknas och sätts in på ditt sparkonto vid årets slut (den 31 december).",
                Category = "Ränta",
                Keywords = new[] { "ränta", "utbetalning", "när", "betalas", "årlig" },
                IsPopular = true
            },
            new FaqEntry
            {
                Id = 2,
                Question = "Hur mycket ränta får jag på mitt sparkonto?",
                Answer = "Räntan varierar mellan konton. Din aktuella ränta visas på varje konto under 'Mina konton'.",
                Category = "Ränta",
                Keywords = new[] { "ränta", "procent", "räntesats", "aktuell" },
                IsPopular = false
            },
            new FaqEntry
            {
                Id = 3,
                Question = "Är räntan fast eller rörlig?",
                Answer = "Räntan är rörlig och kan ändras över tid. Ändringar meddelas i appen.",
                Category = "Ränta",
                Keywords = new[] { "ränta", "fast", "rörlig", "ändras" },
                IsPopular = false
            },
            new FaqEntry
            {
                Id = 4,
                Question = "Hur beräknas räntan?",
                Answer = "Räntan beräknas på ditt saldo och sätts in på kontot vid årets slut.",
                Category = "Ränta",
                Keywords = new[] { "ränta", "beräknas", "beräkning", "saldo" },
                IsPopular = false
            },

            // ===== Insättning & Uttag: 3 entries (ids 5-7) =====
            new FaqEntry
            {
                Id = 5,
                Question = "Hur gör jag ett uttag?",
                Answer = "Uttag görs via appen under 'Mina konton'. Uttaget bokförs normalt inom två bankdagar.",
                Category = "Insättning & Uttag",
                Keywords = new[] { "uttag", "ta ut", "pengar", "överföring" },
                IsPopular = true
            },
            new FaqEntry
            {
                Id = 6,
                Question = "Hur sätter jag in pengar på mitt konto?",
                Answer = "Du sätter in pengar via appen under 'Mina konton' genom att välja kontot och 'Insättning'.",
                Category = "Insättning & Uttag",
                Keywords = new[] { "insättning", "sätta in", "pengar", "konto" },
                IsPopular = false
            },
            new FaqEntry
            {
                Id = 7,
                Question = "Hur lång tid tar en insättning?",
                Answer = "Insättningar bokförs normalt inom två bankdagar.",
                Category = "Insättning & Uttag",
                Keywords = new[] { "insättning", "tid", "bokförs", "bankdagar" },
                IsPopular = false
            },

            // ===== Skatt & Rapporter: 2 entries (ids 8-9) =====
            new FaqEntry
            {
            Id = 8,
            Question = "Var hittar jag mitt skatteunderlag?",
            Answer = "Ditt skatteunderlag finns under 'Dokument'.",
            Category = "Rapporter",
            Keywords = new[] { "skatteunderlag", "skatterapport", "rapport", "deklaration", "årsrapport" },
            IsPopular = true
            },
            new FaqEntry
            {
            Id = 9,
            Question = "Hur laddar jag ner ett kontoutdrag?",
            Answer = "Du kan skapa och ladda ner ett kontoutdrag under 'Dokument' genom att välja konto och tidsperiod.",
            Category = "Rapporter",
            Keywords = new[] { "kontoutdrag", "transaktioner", "rapport", "pdf", "ladda ner" },
            IsPopular = false
            },

            // ===== Konto & Inlogg: 1 entry (id 10) =====
            new FaqEntry
            {
                Id = 10,
                Question = "Hur loggar jag in?",
                Answer = "Du loggar in med din e-postadress och ditt lösenord, eller med BankID.",
                Category = "Konto & Inlogg",
                Keywords = new[] { "logga in", "inloggning", "bankid", "lösenord", "glömt" },
                IsPopular = true
            }
        );

        // English version
        modelBuilder.Entity<FaqEntryTranslation>().HasData(
            // ===== Interest (entries 1-4) =====
            new FaqEntryTranslation
            {
                Id = 1,
                FaqEntryId = 1,
                Language = "en",
                Question = "When is interest paid out?",
                Answer = "Interest is calculated and added to your savings account at the end of the year (31 December).",
                Category = "Interest",
                Keywords = new[] { "interest", "payout", "when", "paid", "yearly" }
            },
            new FaqEntryTranslation
            {
                Id = 2,
                FaqEntryId = 2,
                Language = "en",
                Question = "How much interest do I get on my savings account?",
                Answer = "The interest rate varies between accounts. Your current rate is shown on each account under 'My accounts'.",
                Category = "Interest",
                Keywords = new[] { "interest", "percent", "rate", "current" }
            },
            new FaqEntryTranslation
            {
                Id = 3,
                FaqEntryId = 3,
                Language = "en",
                Question = "Is the interest rate fixed or variable?",
                Answer = "The interest rate is variable and can change over time. Changes are announced in the app.",
                Category = "Interest",
                Keywords = new[] { "interest", "fixed", "variable", "change" }
            },
            new FaqEntryTranslation
            {
                Id = 4,
                FaqEntryId = 4,
                Language = "en",
                Question = "How is the interest calculated?",
                Answer = "Interest is calculated on your balance and added to the account at the end of the year.",
                Category = "Interest",
                Keywords = new[] { "interest", "calculate", "calculated", "calculation", "balance" }
            },

            // ===== Deposits & Withdrawals (entries 5-7) =====
            new FaqEntryTranslation
            {
                Id = 5,
                FaqEntryId = 5,
                Language = "en",
                Question = "How do I make a withdrawal?",
                Answer = "Withdrawals are made in the app under 'My accounts'. A withdrawal is normally booked within two banking days.",
                Category = "Deposits & Withdrawals",
                Keywords = new[] { "withdrawal", "withdraw", "take out", "money", "transfer" }
            },
            new FaqEntryTranslation
            {
                Id = 6,
                FaqEntryId = 6,
                Language = "en",
                Question = "How do I deposit money into my account?",
                Answer = "You deposit money in the app under 'My accounts' by selecting the account and 'Deposit'.",
                Category = "Deposits & Withdrawals",
                Keywords = new[] { "deposit", "add", "money", "account" }
            },
            new FaqEntryTranslation
            {
                Id = 7,
                FaqEntryId = 7,
                Language = "en",
                Question = "How long does a deposit take?",
                Answer = "Deposits are normally booked within two banking days.",
                Category = "Deposits & Withdrawals",
                Keywords = new[] { "deposit", "time", "booked", "banking", "days" }
            },

            // ===== Tax & Reports (entries 8-9) =====
            new FaqEntryTranslation
            {
                Id = 8,
                FaqEntryId = 8,
                Language = "en",
                Question = "Where can I find my tax report?",
                Answer = "Your tax report is available under 'Documents'.",
                Category = "Reports",
                Keywords = new[] { "tax report", "tax statement", "report", "declaration" }
            },
            new FaqEntryTranslation
            {
                Id = 9,
                FaqEntryId = 9,
                Language = "en",
                Question = "How do I download a bank statement?",
                Answer = "You can generate and download a bank statement under 'Documents' by selecting an account and a time interval.",
                Category = "Reports",
                Keywords = new[] { "bank statement", "transactions", "report", "pdf", "download" }
            },

            // ===== Account & Login (entry 10) =====
            new FaqEntryTranslation
            {
                Id = 10,
                FaqEntryId = 10,
                Language = "en",
                Question = "How do I log in?",
                Answer = "You log in with your email address and password, or with BankID.",
                Category = "Account & Login",
                Keywords = new[] { "login", "log in", "logging", "bankid", "password", "forgot" }
            }
        );

        // Audit Entry
        modelBuilder.Entity<AuditEntry>().HasIndex(a => new { a.CustomerId, a.Timestamp });
    }
}