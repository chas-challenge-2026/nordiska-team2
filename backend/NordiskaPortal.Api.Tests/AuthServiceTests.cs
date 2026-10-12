using Microsoft.Extensions.Configuration;
using NordiskaPortal.Api.Services;
using Xunit;

namespace NordiskaPortal.Api.Tests
{
    [Collection("Postgres collection")]
    public class AuthServiceTests
    {
        private readonly PostgresFixture _fixture;

        public AuthServiceTests(PostgresFixture fixture)
        {
            _fixture = fixture;
        }

        // Explicit test config rather than relying on User Secrets/appsettings —
        // keeps the test self-contained and runnable in CI without any
        // machine-specific setup.
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

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ReturnsTokens()
        {
            await using var db = _fixture.CreateContext();
            var authService = new AuthService(db, BuildTestConfig(), AuditServiceTests.For(db));

            var result = await authService.LoginAsync("anna@example.com", "password123");

            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result!.AccessToken));
            Assert.False(string.IsNullOrEmpty(result.RefreshToken));
        }

        [Fact]
        public async Task LoginAsync_WithWrongPassword_ReturnsNull()
        {
            await using var db = _fixture.CreateContext();
            var authService = new AuthService(db, BuildTestConfig(), AuditServiceTests.For(db));

            var result = await authService.LoginAsync("anna@example.com", "wrong-password");

            Assert.Null(result);
        }

        [Fact]
        public async Task RefreshAsync_RotatesToken_AndInvalidatesThePrevious()
        {
            await using var db = _fixture.CreateContext();
            var authService = new AuthService(db, BuildTestConfig(), AuditServiceTests.For(db));

            var login = await authService.LoginAsync("anna@example.com", "password123");
            Assert.NotNull(login);

            // First refresh should succeed and produce a genuinely new pair.
            var refreshed = await authService.RefreshAsync(login!.RefreshToken);
            Assert.NotNull(refreshed);
            Assert.NotEqual(login.RefreshToken, refreshed!.RefreshToken);
            Assert.NotEqual(login.AccessToken, refreshed.AccessToken);

            // Reusing the ORIGINAL (now-rotated-out) refresh token must fail —
            // this is the actual proof that rotation revokes the old token,
            // not just that a new one happens to get issued alongside it.
            var reuseAttempt = await authService.RefreshAsync(login.RefreshToken);
            Assert.Null(reuseAttempt);
        }

        [Fact]
        public async Task LogoutAsync_RevokesToken_SubsequentRefreshFails()
        {
            await using var db = _fixture.CreateContext();
            var authService = new AuthService(db, BuildTestConfig(), AuditServiceTests.For(db));

            var login = await authService.LoginAsync("anna@example.com", "password123");
            Assert.NotNull(login);

            await authService.LogoutAsync(login!.RefreshToken);

            // This is the stricter check we didn't do manually in the browser —
            // presenting the actual revoked token value (not just "no cookie
            // present") and confirming the server-side revocation itself
            // rejects it, not merely that the client no longer has it to send.
            var result = await authService.RefreshAsync(login.RefreshToken);

            Assert.Null(result);
        }
    }
}