using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NordiskaPortal.Api.Models;
using NordiskaPortal.Api.Services;
using Xunit;

namespace NordiskaPortal.Api.Tests
{
    // Automated versions of the guarantees the audit log was manually
    // verified against. Every test uses its own customer (TestData), so the
    // audit rows it asserts on can't come from any other test.
    [Collection("Postgres collection")]
    public class AuditLogTests
    {
        private readonly PostgresFixture _fixture;

        public AuditLogTests(PostgresFixture fixture)
        {
            _fixture = fixture;
        }

        private async Task<List<AuditEntry>> EntriesForAsync(int customerId)
        {
            await using var db = _fixture.CreateContext();
            return await db.AuditEntries
                .Where(a => a.CustomerId == customerId)
                .OrderBy(a => a.Id)
                .ToListAsync();
        }

        private async Task<string> AccountNumberAsync(int accountId)
        {
            await using var db = _fixture.CreateContext();
            return (await db.SavingsAccounts.FindAsync(accountId))!.AccountNumber;
        }

        // MONEY MOVEMENT
        [Fact]
        public async Task Deposit_WritesExactlyOneAuditEntry()
        {
            var (customerId, account, _) = await TestData.CreateCustomerWithAccountsAsync(_fixture, 0m);

            await using var db = _fixture.CreateContext();
            var result = await new TransactionService(db, AuditServiceTests.For(db)).DepositAsync(account, 500m);
            Assert.True(result.Success, result.Error);

            var entry = Assert.Single(await EntriesForAsync(customerId));
            Assert.Equal(AuditActions.Deposit, entry.Action);
            Assert.Equal(await AccountNumberAsync(account), entry.RefId);
            Assert.Equal(customerId.ToString(), entry.Actor);
        }

        [Fact]
        public async Task Withdrawal_WithInsufficientFunds_WritesNoAuditEntry()
        {
            // A refused action changed nothing, so there is nothing to audit.
            var (customerId, account, _) = await TestData.CreateCustomerWithAccountsAsync(_fixture, 100m);

            await using var db = _fixture.CreateContext();
            var result = await new TransactionService(db, AuditServiceTests.For(db)).WithdrawAsync(account, 1000m);

            Assert.False(result.Success);
            Assert.Empty(await EntriesForAsync(customerId));
        }

        [Fact]
        public async Task Transfer_WritesOneAuditEntry_NamingBothAccounts()
        {
            var (customerId, a, b) = await TestData.CreateCustomerWithAccountsAsync(_fixture, 1000m);

            await using var db = _fixture.CreateContext();
            var result = await new TransactionService(db, AuditServiceTests.For(db)).TransferAsync(a, b, 400m);
            Assert.True(result.Success, result.Error);

            // One business event = one audit row, even though the transfer
            // wrote two ledger rows.
            var entry = Assert.Single(await EntriesForAsync(customerId));
            Assert.Equal(AuditActions.Transfer, entry.Action);
            Assert.Equal($"{await AccountNumberAsync(a)}>{await AccountNumberAsync(b)}", entry.RefId);
        }

        [Fact]
        public async Task Transfer_ToAnotherCustomer_WritesNoAuditEntry()
        {
            var (me, myAccount, _) = await TestData.CreateCustomerWithAccountsAsync(_fixture, 1000m);
            var (them, theirAccount, _) = await TestData.CreateCustomerWithAccountsAsync(_fixture, 0m);

            await using var db = _fixture.CreateContext();
            var result = await new TransactionService(db, AuditServiceTests.For(db)).TransferAsync(myAccount, theirAccount, 100m);

            Assert.False(result.Success);
            Assert.Empty(await EntriesForAsync(me));
            Assert.Empty(await EntriesForAsync(them));
        }


        // ATOMICITY
        [Fact]
        public async Task WhenTheActionFailsToSave_ItsAuditEntryIsNotSavedEither()
        {
            // Forces the database itself to reject the save: the Description
            // column is varchar(100), and the service doesn't check length
            // (the validator does, but this test calls the service directly).
            // Record() has already staged the audit row when SaveChangesAsync
            // throws -- so this proves both rows live and die together.
            var (customerId, account, _) = await TestData.CreateCustomerWithAccountsAsync(_fixture, 0m);
            var tooLong = new string('x', 101);

            await using (var db = _fixture.CreateContext())
            {
                var service = new TransactionService(db, AuditServiceTests.For(db));
                await Assert.ThrowsAnyAsync<DbUpdateException>(() => service.DepositAsync(account, 500m, tooLong));
            }

            await using var verifyDb = _fixture.CreateContext();
            Assert.Equal(0, await verifyDb.Transactions.CountAsync(t => t.AccountId == account));
            Assert.Empty(await EntriesForAsync(customerId));
        }

        // LOGIN
        private static IConfiguration BuildTestConfig() =>
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-signing-key-at-least-32-bytes-long-for-hmacsha256",
                ["Jwt:Issuer"] = "NordiskaPortal.Tests",
                ["Jwt:Audience"] = "NordiskaPortal.Tests.Client",
                ["Jwt:AccessTokenExpiryMinutes"] = "15",
                ["Jwt:RefreshTokenExpiryDays"] = "7",
            }).Build();

        // TestData customers have a dummy password hash, so login tests
        // create a customer with a real one. Work factor 4 (the minimum)
        // keeps the test fast; production hashes are far stronger.
        private async Task<(int CustomerId, string Email)> CreateLoginCustomerAsync(string password)
        {
            await using var db = _fixture.CreateContext();
            var unique = Guid.NewGuid().ToString("N")[..8];
            var customer = new Customer
            {
                Name = $"Login {unique}",
                PersonalId = $"19{Random.Shared.Next(100000, 999999)}-{Random.Shared.Next(1000, 9999)}",
                Address = "Testgatan 2",
                Email = $"login{unique}@test.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 4),
                CreatedAt = DateTime.UtcNow
            };
            db.Customers.Add(customer);
            await db.SaveChangesAsync();
            return (customer.Id, customer.Email);
        }

        [Fact]
        public async Task SuccessfulLogin_WritesLoginEntry()
        {
            var (customerId, email) = await CreateLoginCustomerAsync("secret123");

            await using var db = _fixture.CreateContext();
            var result = await new AuthService(db, BuildTestConfig(), AuditServiceTests.For(db)).LoginAsync(email, "secret123");
            Assert.NotNull(result);

            var entry = Assert.Single(await EntriesForAsync(customerId));
            Assert.Equal(AuditActions.Login, entry.Action);
            Assert.Equal(customerId.ToString(), entry.Actor);
        }

        [Fact]
        public async Task WrongPassword_WritesLoginFailed_ConcerningTheCustomer_ByAnonymousActor()
        {
            var (customerId, email) = await CreateLoginCustomerAsync("secret123");

            await using var db = _fixture.CreateContext();
            var result = await new AuthService(db, BuildTestConfig(), AuditServiceTests.For(db)).LoginAsync(email, "wrong");
            Assert.Null(result);

            // Concerns this customer (so they can see it), but whoever typed
            // the password is not known to BE them.
            var entry = Assert.Single(await EntriesForAsync(customerId));
            Assert.Equal(AuditActions.LoginFailed, entry.Action);
            Assert.Equal("anonymous", entry.Actor);
        }

        [Fact]
        public async Task UnknownEmail_WritesLoginFailed_WithoutStoringWhatWasTyped()
        {
            var typed = $"nobody-{Guid.NewGuid():N}@test.local";

            await using var db = _fixture.CreateContext();
            var lastIdBefore = await db.AuditEntries.MaxAsync(a => (int?)a.Id) ?? 0;

            var result = await new AuthService(db, BuildTestConfig(), AuditServiceTests.For(db)).LoginAsync(typed, "whatever");
            Assert.Null(result);

            await using var verifyDb = _fixture.CreateContext();
            var newEntries = await verifyDb.AuditEntries.Where(a => a.Id > lastIdBefore).ToListAsync();

            var entry = Assert.Single(newEntries);
            Assert.Equal(AuditActions.LoginFailed, entry.Action);
            Assert.Null(entry.CustomerId);
            Assert.Null(entry.RefId);

            // The typed text must not appear anywhere in the row: people
            // type passwords into the email field by mistake.
            Assert.DoesNotContain(newEntries, e =>
                (e.Actor + e.RefId + e.Action).Contains(typed, StringComparison.OrdinalIgnoreCase));
        }

        // READ PATH (GET /api/audit)
        [Fact]
        public async Task GetForCustomer_ReturnsOnlyThatCustomersEntries()
        {
            var (anna, annaAccount, _) = await TestData.CreateCustomerWithAccountsAsync(_fixture, 0m);
            var (_, erikAccount, _) = await TestData.CreateCustomerWithAccountsAsync(_fixture, 0m);

            await using (var db = _fixture.CreateContext())
            {
                var service = new TransactionService(db, AuditServiceTests.For(db));
                await service.DepositAsync(annaAccount, 100m);
                await service.DepositAsync(erikAccount, 200m);
            }

            var annasAccountNumber = await AccountNumberAsync(annaAccount);
            var eriksAccountNumber = await AccountNumberAsync(erikAccount);

            await using var readDb = _fixture.CreateContext();
            var annasLog = await AuditServiceTests.For(readDb).GetForCustomerAsync(anna);

            // Exactly her own deposit -- and nothing of Erik's, even though
            // his entry exists in the same table.
            var entry = Assert.Single(annasLog);
            Assert.Equal(annasAccountNumber, entry.RefId);
            Assert.DoesNotContain(annasLog, e => e.RefId == eriksAccountNumber);
        }

        [Fact]
        public async Task GetForCustomer_HidesSafetyNetRows()
        {
            // unaudited_request rows are for developers, not customers.
            var (customerId, account, _) = await TestData.CreateCustomerWithAccountsAsync(_fixture, 0m);

            await using (var db = _fixture.CreateContext())
            {
                var audit = AuditServiceTests.For(db);
                audit.Record(AuditActions.UnauditedRequest, customerId, "POST api/something 200");
                await db.SaveChangesAsync();

                await new TransactionService(db, AuditServiceTests.For(db)).DepositAsync(account, 100m);
            }

            await using var readDb = _fixture.CreateContext();
            var log = await AuditServiceTests.For(readDb).GetForCustomerAsync(customerId);

            var entry = Assert.Single(log);
            Assert.Equal(AuditActions.Deposit, entry.Action);
        }


        // SAFETY NET RELIES ON
        [Fact]
        public async Task RecordCount_CountsStagedEntries()
        {
            var (customerId, _, _) = await TestData.CreateCustomerWithAccountsAsync(_fixture, 0m);

            await using var db = _fixture.CreateContext();
            var audit = AuditServiceTests.For(db);

            Assert.Equal(0, audit.RecordCount);
            audit.Record(AuditActions.Login, customerId);
            audit.Record(AuditActions.Logout, customerId);
            Assert.Equal(2, audit.RecordCount);
        }
    }
}