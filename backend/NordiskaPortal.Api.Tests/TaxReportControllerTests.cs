using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NordiskaPortal.Api.Data;
using NordiskaPortal.Api.Models;
using Xunit;

namespace NordiskaPortal.Api.Tests.Integration
{
    public class TaxReportControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly string _dbName;

        public TaxReportControllerTests(WebApplicationFactory<Program> factory)
        {
            _dbName = "IntegrationTestDb_" + Guid.NewGuid();

            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<BankContext>));

                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<BankContext>(options =>
                        options.UseInMemoryDatabase(_dbName));
                });
            });

            SeedDatabase();
        }

        private void SeedDatabase()
        {
            // Create a scope: BankContext is registered as Scoped, so we
            // can't resolve it directly from the root service provider — we
            // need our own scope, same as a real HTTP request would get.
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BankContext>();

            db.Customers.Add(new Customer
            {
                Id = 1,
                Name = "Anna Testsson",
                PersonalId = "19850505-1234",
                Address = "Testgatan 1, 111 22 Stockholm",
                Email = "anna.test@example.com",
                PasswordHash = "hash",
                CreatedAt = DateTime.UtcNow
            });

            db.SavingsAccounts.Add(new SavingsAccount
            {
                Id = 1,
                CustomerId = 1,
                AccountNumber = "NKM-TEST01",
                InterestRate = 0.035m,
                AccountType = "Savings"
            });

            db.Transactions.Add(new Transaction
            {
                Id = 1,
                AccountId = 1,
                Type = TransactionType.Deposit,
                Amount = 10000m,
                Description = "Test deposit",
                TransactionDate = new DateTime(2023, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                PostingDate = new DateTime(2023, 3, 3, 0, 0, 0, DateTimeKind.Utc),
                Status = TransactionStatus.Posted
            });

            db.SaveChanges();
        }

        // Verifies that requesting a tax report for the current year returns BadRequest.
        [Fact]
        public async Task GetReport_ReturnsBadRequest_ForCurrentYear()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync(
                $"/api/tax-reports/1/{DateTime.UtcNow.Year}");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // Verifies that requesting a tax report that has not been generated returns NotFound.
        [Fact]
        public async Task GetReport_ReturnsNotFound_WhenNoReportGeneratedYet()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync(
                "/api/tax-reports/1/2020");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // Verifies that an account with no generated tax reports returns an empty array of available years.
        [Fact]
        public async Task GetAvailableYears_ReturnsEmptyArray_WhenNoneGenerated()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync(
                "/api/tax-reports/1/available-years");

            response.EnsureSuccessStatusCode();

            var years = await response.Content.ReadFromJsonAsync<int[]>();

            Assert.NotNull(years);
            Assert.Empty(years!);
        }

        // Verifies that an admin can generate a tax report and retrieve it as a non-empty PDF.
        [Fact]
        public async Task AdminGenerateReport_ThenGetReport_ReturnsGeneratedPdf()
        {
            var client = _factory.CreateClient();

            var generateResponse = await client.PostAsync(
                "/api/tax-reports/admin/generate/1/2023",
                null);

            Assert.Equal(HttpStatusCode.OK, generateResponse.StatusCode);

            var getResponse = await client.GetAsync(
                "/api/tax-reports/1/2023");

            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.Equal(
                "application/pdf",
                getResponse.Content.Headers.ContentType?.MediaType);

            var bytes = await getResponse.Content.ReadAsByteArrayAsync();
            Assert.NotEmpty(bytes);
        }

        // Verifies that attempting to generate the same tax report twice returns Conflict on the second attempt.
        [Fact]
        public async Task AdminGenerateReport_Twice_ReturnsConflictOnSecondCall()
        {
            var client = _factory.CreateClient();

            await client.PostAsync(
                "/api/tax-reports/admin/generate/1/2023",
                null);

            var secondAttempt = await client.PostAsync(
                "/api/tax-reports/admin/generate/1/2023",
                null);

            Assert.Equal(HttpStatusCode.Conflict, secondAttempt.StatusCode);
        }
    }
}
