using Microsoft.EntityFrameworkCore;
using NordiskaPortal.Api.Data;
using Testcontainers.PostgreSql;
using Xunit;

namespace NordiskaPortal.Api.Tests
{
    // Shared across all tests in a collection (see [CollectionDefinition] below)
    // so the container only starts once per test run, not once per test.
    public class PostgresFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:15")
            .WithDatabase("nordiska_test")
            .WithUsername("nordiska")
            .WithPassword("nordiska123")
            .Build();

        public string ConnectionString => _container.GetConnectionString();

        public async Task InitializeAsync()
        {
            await _container.StartAsync();

            // Apply the real migrations against the fresh container, so the
            // schema (and seed data) matches exactly what production would get.
            var options = new DbContextOptionsBuilder<BankContext>()
                .UseNpgsql(ConnectionString)
                .Options;

            await using var context = new BankContext(options);
            await context.Database.MigrateAsync();
        }

        public async Task DisposeAsync()
        {
            await _container.DisposeAsync();
        }

        // Convenience: a fresh, independently-tracked BankContext, so concurrent
        // operations in a test don't share one DbContext's change tracker
        // (DbContext isn't thread-safe — this mirrors two separate real requests,
        // each getting their own scoped DbContext via DI in the real app).
        public BankContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<BankContext>()
                .UseNpgsql(ConnectionString)
                .Options;
            return new BankContext(options);
        }
    }

    [CollectionDefinition("Postgres collection")]
    public class PostgresCollection : ICollectionFixture<PostgresFixture> { }
}