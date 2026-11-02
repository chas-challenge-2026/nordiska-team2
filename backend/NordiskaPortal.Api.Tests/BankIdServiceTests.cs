using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Time.Testing;
using NordiskaPortal.Api.Data;
using NordiskaPortal.Api.Services;
using NordiskaPortal.Api.Services.BankId;
using Xunit;

namespace NordiskaPortal.Api.Tests
{
    // Completion paths of the mock BankID: these reach the real database and the real AuthService.
    // Order-flow paths that never touch the database are in MockBankIdServiceTests.
    [Collection("Postgres collection")]
    public class MockBankIdServiceCompletionTests
    {
        private const string AnnaPersonalId = "19850505-1234"; // seeded, customer id 1

        private readonly PostgresFixture _fixture;
        private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 27, 12, 0, 0, TimeSpan.Zero));
        private readonly BankIdOrderStore _store;

        public MockBankIdServiceCompletionTests(PostgresFixture fixture)
        {
            _fixture = fixture;
            _store = new BankIdOrderStore(_time);
        }

        private static IConfiguration BuildTestConfig()
        {
            var settings = new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-signing-key-at-least-32-bytes-long-for-hmacsha256",
                ["Jwt:Issuer"] = "NordiskaPortal.Tests",
                ["Jwt:Audience"] = "NordiskaPortal.Tests.Client",
                ["Jwt:AccessTokenExpiryMinutes"] = "15",
                ["Jwt:RefreshTokenExpiryDays"] = "7",
            };
            return new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        }

        // One BankContext shared by the mock and AuthService, like one HTTP request in Program.cs.
        private MockBankIdService CreateService(BankContext db) =>
            new(_store, _time, db, new AuthService(db, BuildTestConfig(), AuditServiceTests.For(db)));

        // Starts an order and plays the user's phone approving it.
        private static async Task<string> StartAndApproveAsync(MockBankIdService service, string personalId)
        {
            var start = await service.StartAsync("127.0.0.1");
            Assert.True(service.Approve(start.OrderRef, personalId));
            return start.OrderRef;
        }

        [Fact]
        public async Task Approved_WithKnownPersonalId_CompletesWithRealTokens()
        {
            await using var db = _fixture.CreateContext();
            var service = CreateService(db);
            var orderRef = await StartAndApproveAsync(service, AnnaPersonalId);

            var result = await service.GetStatusAsync(orderRef);

            Assert.Equal("complete", result.Status);
            Assert.False(string.IsNullOrEmpty(result.AccessToken));
            Assert.False(string.IsNullOrEmpty(result.RefreshToken));
        }

        [Fact]
        public async Task Approved_WithUnknownPersonalId_FailsWithNotACustomer()
        {
            await using var db = _fixture.CreateContext();
            var service = CreateService(db);
            var orderRef = await StartAndApproveAsync(service, "00000000-0000"); // no matching customer

            var result = await service.GetStatusAsync(orderRef);

            Assert.Equal("failed", result.Status);
            Assert.Equal(BankIdHintCodes.NotACustomer, result.HintCode);
            Assert.Null(result.AccessToken);
        }

        [Fact]
        public async Task PolledAgainAfterCompletion_Fails()
        {
            // Orders are one-time use: the same orderRef must never resolve twice.
            await using var db = _fixture.CreateContext();
            var service = CreateService(db);
            var orderRef = await StartAndApproveAsync(service, AnnaPersonalId);

            var first = await service.GetStatusAsync(orderRef);
            var second = await service.GetStatusAsync(orderRef);

            Assert.Equal("complete", first.Status);
            Assert.Equal("failed", second.Status);
        }

        [Fact]
        public async Task IssuedToken_HasCorrectCustomerIdClaim()
        {
            // The design goal from §5b: a BankID-issued token is a real, correctly signed
            // token for the RIGHT customer, not a placeholder.
            await using var db = _fixture.CreateContext();
            var service = CreateService(db);
            var orderRef = await StartAndApproveAsync(service, AnnaPersonalId);

            var result = await service.GetStatusAsync(orderRef);

            var token = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);
            Assert.Equal("1", token.Claims.First(c => c.Type == "sub").Value);
        }
    }
}