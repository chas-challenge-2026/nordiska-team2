using Microsoft.EntityFrameworkCore;
using NordiskaPortal.Api.Data;
using NordiskaPortal.Api.Models;
using NordiskaPortal.Api.Services;
using Xunit;

namespace NordiskaPortal.Api.Tests
{
    // Opening and closing savings accounts. Every test uses its own customer
    // (TestData), so balances, goals and audit rows can't leak between tests.
    [Collection("Postgres collection")]
    public class AccountLifecycleTests
    {
        // Mirrors AccountService.MaxOpenAccounts. If that limit changes, change it here too.
        // The test failing is the reminder.
        private const int MaxOpenAccounts = 10;

        private readonly PostgresFixture _fixture;

        public AccountLifecycleTests(PostgresFixture fixture)
        {
            _fixture = fixture;
        }

        // Same BankContext for every service, exactly like one HTTP request in production.
        // The staged audit rows are saved by the same SaveChangesAsync as the change they describe.
        private static AccountService AccountServiceFor(BankContext db) =>
            new(db, TransactionServiceFor(db), AuditServiceTests.For(db));

        private static TransactionService TransactionServiceFor(BankContext db) =>
            new(db, AuditServiceTests.For(db));

        private async Task<SavingsAccount> LoadAccountAsync(int accountId)
        {
            await using var db = _fixture.CreateContext();
            return await db.SavingsAccounts.AsNoTracking().SingleAsync(a => a.Id == accountId);
        }

        private async Task<List<string>> AuditActionsForAsync(int customerId)
        {
            await using var db = _fixture.CreateContext();
            return await db.AuditEntries
                .Where(a => a.CustomerId == customerId)
                .OrderBy(a => a.Id)
                .Select(a => a.Action)
                .ToListAsync();
        }

        private async Task<int> AddGoalAsync(int customerId, int? accountId, string name)
        {
            await using var db = _fixture.CreateContext();
            var goal = new SavingsGoal
            {
                CustomerId = customerId,
                AccountId = accountId,
                Name = name,
                TargetAmount = 10000m,
                CreatedAt = DateTime.UtcNow
            };
            db.SavingsGoals.Add(goal);
            await db.SaveChangesAsync();
            return goal.Id;
        }

        private async Task<bool> GoalExistsAsync(int goalId)
        {
            await using var db = _fixture.CreateContext();
            return await db.SavingsGoals.AnyAsync(g => g.Id == goalId);
        }

        // Open account
        [Fact]
        public async Task OpenAccount_CreatesEmptyAccount_WithServerChosenTerms()
        {
            var (customerId, _, _) = await TestData.CreateCustomerWithAccountsAsync(_fixture, 0m);

            await using var db = _fixture.CreateContext();
            var result = await AccountServiceFor(db).OpenAccountAsync(customerId);

            Assert.True(result.Success, result.Error);
            var account = result.Account!;
            Assert.Equal(0m, account.Balance);
            Assert.Equal(0.0350m, account.InterestRate);   // the bank's rate, not the caller's
            Assert.Matches(@"^NKM-\d{8}$", account.AccountNumber);

            // Visible in the customer's account list...
            await using var readDb = _fixture.CreateContext();
            var accounts = await AccountServiceFor(readDb).GetAccountsForCustomerAsync(customerId);
            Assert.Contains(accounts, a => a.Id == account.Id);

            // ...and audited with its account number.
            await using var auditDb = _fixture.CreateContext();
            var entry = await auditDb.AuditEntries.SingleAsync(a =>
                a.CustomerId == customerId && a.Action == AuditActions.AccountOpened);
            Assert.Equal(account.AccountNumber, entry.RefId);
        }

        [Fact]
        public async Task OpenAccount_AtTheLimit_IsRefused()
        {
            // TestData already gave this customer 2 accounts.
            var (customerId, _, _) = await TestData.CreateCustomerWithAccountsAsync(_fixture, 0m);

            await using var db = _fixture.CreateContext();
            var service = AccountServiceFor(db);

            for (var i = 2; i < MaxOpenAccounts; i++)
            {
                var ok = await service.OpenAccountAsync(customerId);
                Assert.True(ok.Success, ok.Error);
            }

            var refused = await service.OpenAccountAsync(customerId);

            Assert.False(refused.Success);
            Assert.Contains($"högst {MaxOpenAccounts}", refused.Error);

            await using var readDb = _fixture.CreateContext();
            Assert.Equal(MaxOpenAccounts,
                (await AccountServiceFor(readDb).GetAccountsForCustomerAsync(customerId)).Count);
        }

        [Fact]
        public async Task ClosingAnAccount_FreesASlotUnderTheLimit()
        {
            // Closed accounts must not count towards the limit, or a customer
            // who closed accounts could never open new ones.
            var (customerId, _, emptyAccount) = await TestData.CreateCustomerWithAccountsAsync(_fixture, 0m);

            await using var db = _fixture.CreateContext();
            var service = AccountServiceFor(db);

            for (var i = 2; i < MaxOpenAccounts; i++)
                Assert.True((await service.OpenAccountAsync(customerId)).Success);

            Assert.True((await service.CloseAccountAsync(customerId, emptyAccount)).Success);

            var reopened = await service.OpenAccountAsync(customerId);
            Assert.True(reopened.Success, reopened.Error);
        }

        // Close account
        [Fact]
        public async Task CloseAccount_WithMoney_IsRefused_AndNothingChanges()
        {
            var (customerId, funded, _) = await TestData.CreateCustomerWithAccountsAsync(_fixture, 1000m);
            var goalId = await AddGoalAsync(customerId, funded, "Resa");

            await using (var db = _fixture.CreateContext())
            {
                var result = await AccountServiceFor(db).CloseAccountAsync(customerId, funded);

                Assert.False(result.Success);
                Assert.Contains("tomt", result.Error);
            }

            // A refused close removes nothing: account open, goal kept, no audit.
            Assert.Null((await LoadAccountAsync(funded)).ClosedAt);
            Assert.True(await GoalExistsAsync(goalId));
            Assert.DoesNotContain(AuditActions.AccountClosed, await AuditActionsForAsync(customerId));
        }

        [Fact]
        public async Task CloseAccount_WhenEmpty_ClosesIt_ButKeepsTheRowAndItsHistory()
        {
            // B gets 500 in and 500 out: empty, but with two ledger rows that
            // must survive the close (transactions are immutable).
            var (customerId, a, b) = await TestData.CreateCustomerWithAccountsAsync(_fixture, 1000m);

            await using (var db = _fixture.CreateContext())
            {
                var transactions = TransactionServiceFor(db);
                Assert.True((await transactions.TransferAsync(a, b, 500m)).Success);
                Assert.True((await transactions.TransferAsync(b, a, 500m)).Success);
            }

            await using (var db = _fixture.CreateContext())
            {
                var result = await AccountServiceFor(db).CloseAccountAsync(customerId, b);
                Assert.True(result.Success, result.Error);
            }

            // Closed, not deleted.
            Assert.NotNull((await LoadAccountAsync(b)).ClosedAt);

            await using var readDb = _fixture.CreateContext();
            Assert.Equal(2, await readDb.Transactions.CountAsync(t => t.AccountId == b));

            // Gone from the customer's view, and fails every ownership check.
            var service = AccountServiceFor(readDb);
            Assert.DoesNotContain(await service.GetAccountsForCustomerAsync(customerId), x => x.Id == b);
            Assert.False(await service.CustomerOwnsAccountAsync(customerId, b));

            Assert.Contains(AuditActions.AccountClosed, await AuditActionsForAsync(customerId));
        }

        [Fact]
        public async Task CloseAccount_RemovesLinkedGoals_AuditsEach_AndKeepsTheRest()
        {
            var (customerId, keptAccount, closedAccount) = await TestData.CreateCustomerWithAccountsAsync(_fixture, 0m);

            var linkedToClosed = await AddGoalAsync(customerId, closedAccount, "Buffert");
            var linkedToOther = await AddGoalAsync(customerId, keptAccount, "Resa");
            var unlinked = await AddGoalAsync(customerId, null, "Bil");

            await using (var db = _fixture.CreateContext())
            {
                var result = await AccountServiceFor(db).CloseAccountAsync(customerId, closedAccount);
                Assert.True(result.Success, result.Error);
            }

            Assert.False(await GoalExistsAsync(linkedToClosed));
            Assert.True(await GoalExistsAsync(linkedToOther));
            Assert.True(await GoalExistsAsync(unlinked));

            // The removed goal is audited, in the same save as the close.
            await using var auditDb = _fixture.CreateContext();
            var goalDeleted = await auditDb.AuditEntries.SingleAsync(a =>
                a.CustomerId == customerId && a.Action == AuditActions.SavingsGoalDeleted);
            Assert.Equal(linkedToClosed.ToString(), goalDeleted.RefId);
            Assert.Contains(AuditActions.AccountClosed, await AuditActionsForAsync(customerId));
        }

        [Fact]
        public async Task CloseAccount_BelongingToAnotherCustomer_IsRefused()
        {
            // Calls the service directly, skipping the controller's ownership
            // check, to prove the service refuses by itself.
            var (_, theirEmptyAccount, _) = await TestData.CreateCustomerWithAccountsAsync(_fixture, 0m);
            var (me, _, _) = await TestData.CreateCustomerWithAccountsAsync(_fixture, 0m);

            await using (var db = _fixture.CreateContext())
            {
                var result = await AccountServiceFor(db).CloseAccountAsync(me, theirEmptyAccount);

                Assert.False(result.Success);
                Assert.Equal("Kontot kunde inte hittas.", result.Error);
            }

            Assert.Null((await LoadAccountAsync(theirEmptyAccount)).ClosedAt);
        }

        [Fact]
        public async Task CloseAccount_Twice_SecondAttemptIsRefused()
        {
            var (customerId, _, empty) = await TestData.CreateCustomerWithAccountsAsync(_fixture, 0m);

            await using var db = _fixture.CreateContext();
            var service = AccountServiceFor(db);

            Assert.True((await service.CloseAccountAsync(customerId, empty)).Success);

            var second = await service.CloseAccountAsync(customerId, empty);
            Assert.False(second.Success);
            Assert.Equal("Kontot kunde inte hittas.", second.Error);

            // Exactly one close in the audit log, not two.
            Assert.Single((await AuditActionsForAsync(customerId)).Where(a => a == AuditActions.AccountClosed));
        }

        // Closed account stays closed for money
        [Fact]
        public async Task ClosedAccount_RefusesDeposits()
        {
            var (customerId, _, closed) = await TestData.CreateCustomerWithAccountsAsync(_fixture, 0m);

            await using (var db = _fixture.CreateContext())
                Assert.True((await AccountServiceFor(db).CloseAccountAsync(customerId, closed)).Success);

            await using (var db = _fixture.CreateContext())
            {
                // Service-level guard.
                // The controller's ownership check would also refuse, but this proves the service doesn't rely on it.
                var result = await TransactionServiceFor(db).DepositAsync(closed, 500m);

                Assert.False(result.Success);
                Assert.Equal("Kontot kunde inte hittas.", result.Error);
            }

            await using var readDb = _fixture.CreateContext();
            Assert.Equal(0, await readDb.Transactions.CountAsync(t => t.AccountId == closed));
        }

        [Fact]
        public async Task ClosedAccount_CannotReceiveTransfers()
        {
            var (customerId, open, closed) = await TestData.CreateCustomerWithAccountsAsync(_fixture, 1000m);

            await using (var db = _fixture.CreateContext())
                Assert.True((await AccountServiceFor(db).CloseAccountAsync(customerId, closed)).Success);

            await using (var db = _fixture.CreateContext())
            {
                var result = await TransactionServiceFor(db).TransferAsync(open, closed, 500m);
                Assert.False(result.Success);
            }

            await using var readDb = _fixture.CreateContext();
            Assert.Equal(1000m, await TransactionServiceFor(readDb).GetBalanceAsync(open));
            Assert.Equal(0, await readDb.Transactions.CountAsync(t => t.AccountId == closed));
        }
    }
}