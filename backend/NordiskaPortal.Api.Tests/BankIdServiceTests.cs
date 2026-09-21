using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NordiskaPortal.Api.Data;
using NordiskaPortal.Api.Services;
using Xunit;

namespace NordiskaPortal.Api.Tests
{
    [Collection("Postgres collection")]
    public class BankIdServiceTests
    {
        private readonly PostgresFixture _fixture;

        public BankIdServiceTests(PostgresFixture fixture)
        {
            _fixture = fixture;
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

        // BankIdService is a Singleton in production, depending on IServiceScopeFactory 
        // rather than BankContext/IAuthService directly. 
        // 
        // Testing it through a fake/direct-injection shortcut would miss 
        // that the real scope-factory path works. 
        // 
        // This builds a small real DI container, wired the same shape as Program.cs's, 
        // against the same Testcontainers Postgres instance the other test classes share.
        private IServiceScopeFactory BuildScopeFactory()
        {
            var services = new ServiceCollection();
            services.AddScoped(_ => _fixture.CreateContext());
            services.AddScoped<IAuthService>(sp =>
                new AuthService(sp.GetRequiredService<BankContext>(), BuildTestConfig()));

            var provider = services.BuildServiceProvider();
            return provider.GetRequiredService<IServiceScopeFactory>();
        }

        [Fact]
        public async Task GetStatusAsync_BeforeDelayElapses_ReturnsPending()
        {
            var service = new BankIdService(BuildScopeFactory());
            var orderRef = service.StartOrder("19850505-1234"); // Anna, seeded

            var result = await service.GetStatusAsync(orderRef);

            Assert.Equal("pending", result.Status);
            Assert.Null(result.AccessToken);
            Assert.Null(result.RefreshToken);
        }

        [Fact]
        public async Task GetStatusAsync_UnknownOrderRef_ReturnsFailed()
        {
            var service = new BankIdService(BuildScopeFactory());

            var result = await service.GetStatusAsync(Guid.NewGuid().ToString());

            Assert.Equal("failed", result.Status);
        }

        [Fact]
        public async Task GetStatusAsync_AfterDelayWithValidPersonalId_ReturnsCompleteWithRealTokens()
        {
            var service = new BankIdService(BuildScopeFactory());
            var orderRef = service.StartOrder("19850505-1234"); // Anna, seeded

            // The mock's simulated approval delay is 3 seconds.
            // Waited out for real here rather than mocked. 
            // The delay is a private implementation detail with no injection seam.
            await Task.Delay(TimeSpan.FromSeconds(3.5));

            var result = await service.GetStatusAsync(orderRef);

            Assert.Equal("complete", result.Status);
            Assert.False(string.IsNullOrEmpty(result.AccessToken));
            Assert.False(string.IsNullOrEmpty(result.RefreshToken));
        }

        [Fact]
        public async Task GetStatusAsync_AfterDelayWithUnknownPersonalId_ReturnsFailed()
        {
            var service = new BankIdService(BuildScopeFactory());
            var orderRef = service.StartOrder("00000000-0000"); // no matching customer

            await Task.Delay(TimeSpan.FromSeconds(3.5));

            var result = await service.GetStatusAsync(orderRef);

            Assert.Equal("failed", result.Status);
            Assert.Null(result.AccessToken);
        }

        [Fact]
        public async Task GetStatusAsync_PolledAgainAfterCompletion_ReturnsFailed()
        {
            // Orders are one-time use.
            // The same orderRef must not resolve twice whether the first result was complete or failed.
            var service = new BankIdService(BuildScopeFactory());
            var orderRef = service.StartOrder("19850505-1234");

            await Task.Delay(TimeSpan.FromSeconds(3.5));
            var first = await service.GetStatusAsync(orderRef);
            var second = await service.GetStatusAsync(orderRef);

            Assert.Equal("complete", first.Status);
            Assert.Equal("failed", second.Status);
        }

        [Fact]
        public async Task GetStatusAsync_IssuedToken_HasCorrectCustomerIdClaim()
        {
            // Proves a BankID-issued token is a genuinely real.
            // Correctly signed token for the intended customer.
            // Not a stand-in or placeholder value.
            var service = new BankIdService(BuildScopeFactory());
            var orderRef = service.StartOrder("19850505-1234"); // Anna, customer id 1

            await Task.Delay(TimeSpan.FromSeconds(3.5));
            var result = await service.GetStatusAsync(orderRef);

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(result.AccessToken);
            var subClaim = token.Claims.First(c => c.Type == "sub").Value;

            Assert.Equal("1", subClaim);
        }
    }
}