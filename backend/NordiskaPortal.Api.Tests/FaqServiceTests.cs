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
                    Question = "När betalas räntan ut?",
                    Answer = "Räntan sätts in vid årets slut.",
                    Category = "Ränta",
                    Keywords = new[] { "ränta", "utbetalning", "när", "betalas", "årlig" }
                },
                new FaqEntry
                {
                    Id = 2,
                    Question = "Hur gör jag ett uttag?",
                    Answer = "Uttag görs via appen.",
                    Category = "Insättning & Uttag",
                    Keywords = new[] { "uttag", "ta ut", "pengar", "överföring" }
                },
                new FaqEntry
                {
                    Id = 3,
                    Question = "Var hittar jag min årsrapport?",
                    Answer = "Under 'Mina rapporter'.",
                    Category = "Skatt & Rapporter",
                    Keywords = new[] { "årsrapport", "rapport", "skatt", "deklaration" }
                },
                new FaqEntry
                {
                    Id = 4,
                    Question = "Hur öppnar jag ett nytt sparkonto?",
                    Answer = "Ansök i appen under 'Nytt konto'.",
                    Category = "Konto & Inlogg",
                    Keywords = new[] { "öppna", "nytt", "sparkonto", "konto", "ansök" }
                },
                new FaqEntry
                {
                    Id = 5,
                    Question = "Hur sätter jag in pengar på mitt konto?",
                    Answer = "Välj kontot och 'Insättning' i appen.",
                    Category = "Insättning & Uttag",
                    Keywords = new[] { "insättning", "sätta in", "pengar", "konto" }
                }
            );

            await db.SaveChangesAsync();
        }

        // Verifies that a single keyword shared by only one entry matches
        // that entry.
        [Fact]
        public async Task SearchAsync_SingleStrongKeyword_MatchesCorrectEntry()
        {
            using var db = CreateContext(
                nameof(SearchAsync_SingleStrongKeyword_MatchesCorrectEntry));

            await SeedFaqAsync(db);

            var service = new FaqService(db);

            var result = await service.SearchAsync(
                new FaqSearchRequest("årsrapport"));

            Assert.True(result.Matched);
            Assert.Equal(
                "Var hittar jag min årsrapport?",
                result.Question);
        }

        // Verifies that a full question is matched to the right entry.
        [Fact]
        public async Task SearchAsync_FullQuestion_MatchesCorrectEntry()
        {
            using var db = CreateContext(
                nameof(SearchAsync_FullQuestion_MatchesCorrectEntry));

            await SeedFaqAsync(db);

            var service = new FaqService(db);

            var result = await service.SearchAsync(
                new FaqSearchRequest("När betalas räntan ut?"));

            Assert.True(result.Matched);
            Assert.Equal(
                "När betalas räntan ut?",
                result.Question);
        }

        // Verifies that when several entries overlap with the query, the
        // best-scoring one wins rather than the first one in the table.
        // Entry 2 shares "pengar" but entry 5 covers all three query words.
        [Fact]
        public async Task SearchAsync_PicksBestScoringEntry_NotFirst()
        {
            using var db = CreateContext(
                nameof(SearchAsync_PicksBestScoringEntry_NotFirst));

            await SeedFaqAsync(db);

            var service = new FaqService(db);

            var result = await service.SearchAsync(
                new FaqSearchRequest("sätta in pengar"));

            Assert.True(result.Matched);
            Assert.Equal(
                "Hur sätter jag in pengar på mitt konto?",
                result.Question);
        }

        // Verifies that a query with no keyword overlap falls back to the
        // no-answer response instead of guessing.
        [Fact]
        public async Task SearchAsync_NoKeywordOverlap_ReturnsNoAnswer()
        {
            using var db = CreateContext(
                nameof(SearchAsync_NoKeywordOverlap_ReturnsNoAnswer));

            await SeedFaqAsync(db);

            var service = new FaqService(db);

            var result = await service.SearchAsync(
                new FaqSearchRequest("vad är vädret idag"));

            Assert.False(result.Matched);
            Assert.Null(result.Question);
        }

        // Verifies that an empty query short-circuits to the no-answer
        // response.
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

        // Verifies that searching an empty FaqEntry table returns the
        // no-answer response instead of throwing.
        [Fact]
        public async Task SearchAsync_NoEntriesInDatabase_ReturnsNoAnswer()
        {
            using var db = CreateContext(
                nameof(SearchAsync_NoEntriesInDatabase_ReturnsNoAnswer));

            var service = new FaqService(db);

            var result = await service.SearchAsync(
                new FaqSearchRequest("ränta"));

            Assert.False(result.Matched);
        }

        // Verifies that the definite form "räntan" matches the keyword
        // "ränta".
        [Fact]
        public async Task SearchAsync_SwedishDefiniteForm_MatchesBaseKeyword()
        {
            using var db = CreateContext(
                nameof(SearchAsync_SwedishDefiniteForm_MatchesBaseKeyword));

            await SeedFaqAsync(db);

            var service = new FaqService(db);

            var result = await service.SearchAsync(
                new FaqSearchRequest("räntan"));

            Assert.True(result.Matched);
            Assert.Equal("Ränta", result.Category);
        }

        // Verifies that the definite plural "räntorna" matches the
        // keyword "ränta".
        [Fact]
        public async Task SearchAsync_SwedishDefinitePlural_MatchesBaseKeyword()
        {
            using var db = CreateContext(
                nameof(SearchAsync_SwedishDefinitePlural_MatchesBaseKeyword));

            await SeedFaqAsync(db);

            var service = new FaqService(db);

            var result = await service.SearchAsync(
                new FaqSearchRequest("räntorna"));

            Assert.True(result.Matched);
            Assert.Equal("Ränta", result.Category);
        }

        // Verifies that "uttaget" (definite) matches the keyword "uttag".
        [Fact]
        public async Task SearchAsync_UttagetMatchesUttag()
        {
            using var db = CreateContext(
                nameof(SearchAsync_UttagetMatchesUttag));

            await SeedFaqAsync(db);

            var service = new FaqService(db);

            var result = await service.SearchAsync(
                new FaqSearchRequest("uttaget"));

            Assert.True(result.Matched);
            Assert.Equal("Hur gör jag ett uttag?", result.Question);
        }

        // Verifies that "insättningen" matches the keyword "insättning"
        // and lands on the deposit entry, not the interest entry.
        [Fact]
        public async Task SearchAsync_InsattningenMatchesInsattning()
        {
            using var db = CreateContext(
                nameof(SearchAsync_InsattningenMatchesInsattning));

            await SeedFaqAsync(db);

            var service = new FaqService(db);

            var result = await service.SearchAsync(
                new FaqSearchRequest("insättningen"));

            Assert.True(result.Matched);
            Assert.Equal(
                "Hur sätter jag in pengar på mitt konto?",
                result.Question);
        }

        // Verifies that different verb forms converge: "betalar" should
        // match the keyword "betalas".
        [Fact]
        public async Task SearchAsync_VerbFormsConverge()
        {
            using var db = CreateContext(
                nameof(SearchAsync_VerbFormsConverge));

            await SeedFaqAsync(db);

            var service = new FaqService(db);

            var result = await service.SearchAsync(
                new FaqSearchRequest("betalar"));

            Assert.True(result.Matched);
            Assert.Equal("Ränta", result.Category);
        }

        // Verifies that filler words do not dilute the score, so a chatty
        // question still matches on its one meaningful word.
        [Fact]
        public async Task SearchAsync_FillerWordsDoNotDiluteScore()
        {
            using var db = CreateContext(
                nameof(SearchAsync_FillerWordsDoNotDiluteScore));

            await SeedFaqAsync(db);

            var service = new FaqService(db);

            var result = await service.SearchAsync(
                new FaqSearchRequest("hur gör jag ett uttag"));

            Assert.True(result.Matched);
            Assert.Equal("Hur gör jag ett uttag?", result.Question);
        }

        // Verifies that letter case and punctuation are ignored.
        [Fact]
        public async Task SearchAsync_IgnoresCaseAndPunctuation()
        {
            using var db = CreateContext(
                nameof(SearchAsync_IgnoresCaseAndPunctuation));

            await SeedFaqAsync(db);

            var service = new FaqService(db);

            var result = await service.SearchAsync(
                new FaqSearchRequest("ÅRSRAPPORT!!!"));

            Assert.True(result.Matched);
            Assert.Equal(
                "Var hittar jag min årsrapport?",
                result.Question);
        }

        // Verifies, against the real seed data, that a login question
        // lands in the one-entry login category.
        [Fact]
        public async Task SeedData_LoginQuestion_MatchesLoginEntry()
        {
            using var db = CreateContext(
                nameof(SeedData_LoginQuestion_MatchesLoginEntry));

            db.Database.EnsureCreated();

            var service = new FaqService(db);

            var result = await service.SearchAsync(
                new FaqSearchRequest("hur loggar jag in"));

            Assert.True(result.Matched);
            Assert.Equal("Konto & Inlogg", result.Category);
        }

        // Verifies, against the real seed data, that the annual report
        // question lands in the tax and reports category.
        [Fact]
        public async Task SeedData_AnnualReportQuestion_MatchesReportEntry()
        {
            using var db = CreateContext(
                nameof(SeedData_AnnualReportQuestion_MatchesReportEntry));

            db.Database.EnsureCreated();

            var service = new FaqService(db);

            var result = await service.SearchAsync(
                new FaqSearchRequest("var hittar jag min årsrapport"));

            Assert.True(result.Matched);
            Assert.Equal("Skatt & Rapporter", result.Category);
        }
    }
}