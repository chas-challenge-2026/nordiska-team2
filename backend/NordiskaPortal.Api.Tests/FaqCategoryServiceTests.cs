using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NordiskaPortal.Api.Controllers;
using NordiskaPortal.Api.Data;
using NordiskaPortal.Api.DTOs;
using NordiskaPortal.Api.Models;
using NordiskaPortal.Api.Services;
using Xunit;

namespace NordiskaPortal.Api.Tests.Services
{
    public class FaqCategoryServiceTests
    {
        private static BankContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<BankContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new BankContext(options);
        }

        private static FaqController CreateController(BankContext db)
        {
            var faqService = new FaqService(db);
            var categoryService = new FaqCategoryService(db);

            return new FaqController(faqService, categoryService);
        }

        private static async Task SeedSmallSetAsync(BankContext db)
        {
            db.FaqEntries.AddRange(
                new FaqEntry { Id = 1, Question = "Q1", Answer = "A1", Category = "Konto & Inlogg", IsPopular = true },
                new FaqEntry { Id = 2, Question = "Q2", Answer = "A2", Category = "Konto & Inlogg", IsPopular = false },
                new FaqEntry { Id = 3, Question = "Q3", Answer = "A3", Category = "Ränta", IsPopular = true },
                // An entry with no category must never produce a card.
                new FaqEntry { Id = 4, Question = "Q4", Answer = "A4", Category = "", IsPopular = false }
            );

            await db.SaveChangesAsync();
        }

        // Verifies that each distinct category becomes one card with the
        // correct number of entries, and that uncategorised entries are skipped.
        [Fact]
        public async Task GetCategoriesAsync_ReturnsOneCardPerCategory_WithCorrectCounts()
        {
            using var db = CreateContext(
                nameof(GetCategoriesAsync_ReturnsOneCardPerCategory_WithCorrectCounts));

            await SeedSmallSetAsync(db);

            var service = new FaqCategoryService(db);

            var categories = await service.GetCategoriesAsync();

            Assert.Equal(2, categories.Count);
            Assert.Equal(
                2,
                categories.Single(c => c.Label == "Konto & Inlogg").QuestionCount);
            Assert.Equal(
                1,
                categories.Single(c => c.Label == "Ränta").QuestionCount);
        }

        // Verifies that category ids are URL-safe: accents removed, '&'
        // dropped, and words joined by a single hyphen.
        [Fact]
        public async Task GetCategoriesAsync_BuildsUrlSafeIds()
        {
            using var db = CreateContext(
                nameof(GetCategoriesAsync_BuildsUrlSafeIds));

            db.FaqEntries.AddRange(
                new FaqEntry { Id = 1, Question = "Q1", Answer = "A1", Category = "Konto & Inlogg" },
                new FaqEntry { Id = 2, Question = "Q2", Answer = "A2", Category = "Insättning & Uttag" },
                new FaqEntry { Id = 3, Question = "Q3", Answer = "A3", Category = "Ränta" },
                new FaqEntry { Id = 4, Question = "Q4", Answer = "A4", Category = "Skatt & Rapporter" }
            );
            await db.SaveChangesAsync();

            var service = new FaqCategoryService(db);

            var categories = await service.GetCategoriesAsync();
            var ids = categories.Select(c => c.Id).ToList();

            Assert.Contains("konto-inlogg", ids);
            Assert.Contains("insattning-uttag", ids);
            Assert.Contains("ranta", ids);
            Assert.Contains("skatt-rapporter", ids);
        }

        // Verifies that asking for one category returns only that
        // category's entries.
        [Fact]
        public async Task GetEntriesByCategoryAsync_ReturnsOnlyEntriesInThatCategory()
        {
            using var db = CreateContext(
                nameof(GetEntriesByCategoryAsync_ReturnsOnlyEntriesInThatCategory));

            await SeedSmallSetAsync(db);

            var service = new FaqCategoryService(db);

            var entries = await service.GetEntriesByCategoryAsync("konto-inlogg");

            Assert.NotNull(entries);
            Assert.Equal(2, entries!.Count);
            Assert.All(entries, e => Assert.Equal("Konto & Inlogg", e.Category));
        }

        // Verifies that an id which matches no category returns null, which
        // the controller turns into a 404.
        [Fact]
        public async Task GetEntriesByCategoryAsync_ReturnsNull_WhenCategoryDoesNotExist()
        {
            using var db = CreateContext(
                nameof(GetEntriesByCategoryAsync_ReturnsNull_WhenCategoryDoesNotExist));

            await SeedSmallSetAsync(db);

            var service = new FaqCategoryService(db);

            var entries = await service.GetEntriesByCategoryAsync("does-not-exist");

            Assert.Null(entries);
        }

        // Verifies that the id lookup ignores letter case and surrounding
        // whitespace.
        [Fact]
        public async Task GetEntriesByCategoryAsync_IgnoresCaseAndWhitespace()
        {
            using var db = CreateContext(
                nameof(GetEntriesByCategoryAsync_IgnoresCaseAndWhitespace));

            await SeedSmallSetAsync(db);

            var service = new FaqCategoryService(db);

            var entries = await service.GetEntriesByCategoryAsync("  RANTA ");

            Assert.NotNull(entries);
            Assert.Single(entries!);
        }

        // Verifies that only entries flagged as popular are returned for the
        // start page.
        [Fact]
        public async Task GetPopularAsync_ReturnsOnlyFlaggedEntries()
        {
            using var db = CreateContext(
                nameof(GetPopularAsync_ReturnsOnlyFlaggedEntries));

            await SeedSmallSetAsync(db);

            var service = new FaqCategoryService(db);

            var popular = await service.GetPopularAsync();

            Assert.Equal(2, popular.Count);
            Assert.Equal(new[] { 1, 3 }, popular.Select(p => p.Id).ToArray());
        }

        // Verifies the real seed data: four categories with 4, 3, 2 and 1
        // entries, so each card shows a visibly different count.
        [Fact]
        public async Task SeedData_HasFourCategories_WithDistinctCounts()
        {
            using var db = CreateContext(
                nameof(SeedData_HasFourCategories_WithDistinctCounts));

            db.Database.EnsureCreated();

            var service = new FaqCategoryService(db);

            var categories = await service.GetCategoriesAsync();
            var counts = categories.ToDictionary(c => c.Id, c => c.QuestionCount);

            Assert.Equal(4, categories.Count);
            Assert.Equal(4, counts["ranta"]);
            Assert.Equal(3, counts["insattning-uttag"]);
            Assert.Equal(2, counts["skatt-rapporter"]);
            Assert.Equal(1, counts["konto-inlogg"]);
        }

        // Verifies that every category card in the real seed data opens a
        // modal holding exactly as many entries as the card claims.
        [Fact]
        public async Task SeedData_EveryCategoryId_ResolvesToItsCountOfEntries()
        {
            using var db = CreateContext(
                nameof(SeedData_EveryCategoryId_ResolvesToItsCountOfEntries));

            db.Database.EnsureCreated();

            var service = new FaqCategoryService(db);

            var categories = await service.GetCategoriesAsync();

            foreach (var category in categories)
            {
                var entries = await service.GetEntriesByCategoryAsync(category.Id);

                Assert.NotNull(entries);
                Assert.Equal(category.QuestionCount, entries!.Count);
            }
        }

        // Verifies that the real seed data marks four questions as popular.
        [Fact]
        public async Task SeedData_HasFourPopularEntries()
        {
            using var db = CreateContext(
                nameof(SeedData_HasFourPopularEntries));

            db.Database.EnsureCreated();

            var service = new FaqCategoryService(db);

            var popular = await service.GetPopularAsync();

            Assert.Equal(4, popular.Count);
        }

        // Verifies that the controller answers 200 with the category list.
        [Fact]
        public async Task Controller_GetCategories_ReturnsOk()
        {
            using var db = CreateContext(
                nameof(Controller_GetCategories_ReturnsOk));

            await SeedSmallSetAsync(db);

            var controller = CreateController(db);

            var result = await controller.GetCategories();

            var ok = Assert.IsType<OkObjectResult>(result);
            var body = Assert.IsType<List<FaqCategoryResponse>>(ok.Value);
            Assert.Equal(2, body.Count);
        }

        // Verifies that the controller answers 404 for an unknown category.
        [Fact]
        public async Task Controller_GetEntries_ReturnsNotFound_ForUnknownCategory()
        {
            using var db = CreateContext(
                nameof(Controller_GetEntries_ReturnsNotFound_ForUnknownCategory));

            await SeedSmallSetAsync(db);

            var controller = CreateController(db);

            var result = await controller.GetEntries("does-not-exist");

            Assert.IsType<NotFoundObjectResult>(result);
        }

        // Verifies that the controller answers 200 with the entries for a
        // known category.
        [Fact]
        public async Task Controller_GetEntries_ReturnsOk_ForKnownCategory()
        {
            using var db = CreateContext(
                nameof(Controller_GetEntries_ReturnsOk_ForKnownCategory));

            await SeedSmallSetAsync(db);

            var controller = CreateController(db);

            var result = await controller.GetEntries("konto-inlogg");

            var ok = Assert.IsType<OkObjectResult>(result);
            var body = Assert.IsType<List<FaqEntryResponse>>(ok.Value);
            Assert.Equal(2, body.Count);
        }
    }
}