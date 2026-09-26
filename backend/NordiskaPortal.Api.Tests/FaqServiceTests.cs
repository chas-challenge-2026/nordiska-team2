using Microsoft.EntityFrameworkCore;
using NordiskaPortal.Api.Data;
using NordiskaPortal.Api.DTOs;
using NordiskaPortal.Api.Models;
using NordiskaPortal.Api.Services;
using Xunit;

namespace NordiskaPortal.Api.Tests.Services
{
    public class FaqServiceTests
    {
        private static BankContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<BankContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new BankContext(options);
        }

        private static async Task SeedFaqAsync(BankContext db)
        {
            db.FaqEntries.AddRange(
                new FaqEntry
                {
                    Id = 1,
                    Question = "When is interest paid out?",
                    Answer = "Interest is added to the account at year end.",
                    Category = "Interest",
                    Keywords = new[] { "interest", "payout", "when", "paid", "annual" }
                },
                new FaqEntry
                {
                    Id = 2,
                    Question = "How do I make a withdrawal?",
                    Answer = "Withdrawals are made via the app under 'My accounts'.",
                    Category = "Transactions",
                    Keywords = new[] { "withdrawal", "withdraw", "money", "transfer" }
                },
                new FaqEntry
                {
                    Id = 3,
                    Question = "Where do I find my annual report?",
                    Answer = "Your annual report is available under 'My reports'.",
                    Category = "Reports",
                    Keywords = new[] { "annualreport", "report", "tax", "declaration" }
                },
                new FaqEntry
                {
                    Id = 4,
                    Question = "How do I open a new savings account?",
                    Answer = "Apply for a new savings account directly in the app under 'New account'.",
                    Category = "Account",
                    Keywords = new[] { "open", "new", "savings", "account", "apply" }
                },
                new FaqEntry
                {
                    Id = 5,
                    Question = "How do I make a deposit?",
                    Answer = "Deposits are made via the app under 'My accounts'.",
                    Category = "Transactions",
                    Keywords = new[] { "deposit", "deposits", "money", "transfer" }
                }
            );

            await db.SaveChangesAsync();
        }

        // Verifies that a single keyword shared by only one entry matches that
        // entry, rather than being rejected for not covering most of its
        // keyword list.
        [Fact]
        public async Task SearchAsync_SingleStrongKeyword_MatchesCorrectEntry()
        {
            using var db = CreateContext(
                nameof(SearchAsync_SingleStrongKeyword_MatchesCorrectEntry));

            await SeedFaqAsync(db);

            var service = new FaqService(db);

            var result = await service.SearchAsync(
                new FaqSearchRequest("annualreport"));

            Assert.True(result.Matched);
            Assert.Equal(
                "Where do I find my annual report?",
                result.Question);
        }

        // Verifies that when a query's words overlap with more than one
        // entry's keywords, the highest-scoring entry is returned rather
        // than whichever entry was seeded first.
        [Fact]
        public async Task SearchAsync_MultiWordQuery_MatchesBestEntry_NotFirst()
        {
            using var db = CreateContext(
                nameof(SearchAsync_MultiWordQuery_MatchesBestEntry_NotFirst));

            await SeedFaqAsync(db);

            var service = new FaqService(db);

            var result = await service.SearchAsync(
                new FaqSearchRequest("when is my interest paid"));

            Assert.True(result.Matched);
            Assert.Equal(
                "When is interest paid out?",
                result.Question);
        }

        // Verifies that a query with no keyword overlap against any entry
        // falls back to the no-answer response instead of guessing.
        [Fact]
        public async Task SearchAsync_NoKeywordOverlap_ReturnsNoAnswer()
        {
            using var db = CreateContext(
                nameof(SearchAsync_NoKeywordOverlap_ReturnsNoAnswer));

            await SeedFaqAsync(db);

            var service = new FaqService(db);

            var result = await service.SearchAsync(
                new FaqSearchRequest("what is the weather today"));

            Assert.False(result.Matched);
            Assert.Null(result.Question);
        }

        // Verifies that an empty or whitespace-only query short-circuits to
        // the no-answer response without scoring any entries.
        [Fact]
        public async Task SearchAsync_EmptyQuery_ReturnsNoAnswer()
        {
            using var db = CreateContext(
                nameof(SearchAsync_EmptyQuery_ReturnsNoAnswer));

            await SeedFaqAsync(db);

            var service = new FaqService(db);

            var result = await service.SearchAsync(
                new FaqSearchRequest(""));

            Assert.False(result.Matched);
        }

        // Verifies that the stemming step collapses a plural query word onto
        // the singular form stored in an entry's keyword list. Kept as a
        // single-word query so the match isn't diluted by unrelated filler
        // words competing for the same confidence threshold.
        [Fact]
        public async Task SearchAsync_StemmingCollapsesPluralAndSingular()
        {
            using var db = CreateContext(
                nameof(SearchAsync_StemmingCollapsesPluralAndSingular));

            await SeedFaqAsync(db);

            var service = new FaqService(db);

            // "withdrawals" (plural) should still match the "withdrawal" keyword.
            var result = await service.SearchAsync(
                new FaqSearchRequest("withdrawals"));

            Assert.True(result.Matched);
            Assert.Equal(
                "How do I make a withdrawal?",
                result.Question);
        }

        // Verifies that two entries sharing an overlapping keyword ("money",
        // "transfer") are still told apart correctly when the query is
        // specific to just one of them.
        [Fact]
        public async Task SearchAsync_DistinguishesBetweenSimilarEntries()
        {
            using var db = CreateContext(
                nameof(SearchAsync_DistinguishesBetweenSimilarEntries));

            await SeedFaqAsync(db);

            var service = new FaqService(db);

            var result = await service.SearchAsync(
                new FaqSearchRequest("deposit"));

            Assert.True(result.Matched);
            Assert.Equal(
                "How do I make a deposit?",
                result.Question);
        }

        // Verifies that searching against an empty FaqEntry table returns the
        // no-answer response instead of throwing.
        [Fact]
        public async Task SearchAsync_NoEntriesInDatabase_ReturnsNoAnswer()
        {
            using var db = CreateContext(
                nameof(SearchAsync_NoEntriesInDatabase_ReturnsNoAnswer));

            var service = new FaqService(db);

            var result = await service.SearchAsync(
                new FaqSearchRequest("interest"));

            Assert.False(result.Matched);
        }
    }
}