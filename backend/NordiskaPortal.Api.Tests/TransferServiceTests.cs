using Microsoft.EntityFrameworkCore;
using NordiskaPortal.Api.Data;
using NordiskaPortal.Api.Models;
using NordiskaPortal.Api.Services;
using Xunit;

namespace NordiskaPortal.Api.Tests
{
    [Collection("Postgres collection")]
    public class TransferServiceTests
    {
        private readonly PostgresFixture _fixture;

        public TransferServiceTests(PostgresFixture fixture)
        {
            _fixture = fixture;
        }

        private static SavingsAccount NewAccount(int customerId, string accountNumber) => new()
        {
            CustomerId = customerId,
            AccountNumber = accountNumber,
            InterestRate = 0.0300m,
            AccountType = "Savings",
            CreatedAt = DateTime.UtcNow
        };

        private async Task<decimal> BalanceAsync(int accountId)
        {
            await using var db = _fixture.CreateContext();
            return await new TransactionService(db, TestAudit.For(db)).GetBalanceAsync(accountId);
        }

        private async Task<int> TransactionCountAsync(int accountId)
        {
            await using var db = _fixture.CreateContext();
            return await db.Transactions.CountAsync(t => t.AccountId == accountId);
        }

        [Fact]
        public async Task Transfer_MovesMoney_AndTotalIsConserved()
        {
            var (_, a, b) = await TestData.CreateCustomerWithAccountsAsync(_fixture, 10000m);

            await using var db = _fixture.CreateContext();
            var result = await new TransactionService(db, TestAudit.For(db)).TransferAsync(a, b, 3000m);

            Assert.True(result.Success, result.Error);
            Assert.Equal(7000m, await BalanceAsync(a));
            Assert.Equal(3000m, await BalanceAsync(b));

            // The defining property of a transfer: money moves, none is
            // created or destroyed.
            Assert.Equal(10000m, await BalanceAsync(a) + await BalanceAsync(b));
        }

        [Fact]
        public async Task Transfer_ShowsCorrectSignInHistoryOnBothSides()
        {
            // Guards the bug found during manual testing: the history used
            // its own sign rule and showed incoming transfers as negative.
            var (_, a, b) = await TestData.CreateCustomerWithAccountsAsync(_fixture, 10000m);

            await using (var db = _fixture.CreateContext())
            {
                var result = await new TransactionService(db, TestAudit.For(db)).TransferAsync(a, b, 1000m);
                Assert.True(result.Success, result.Error);
            }

            await using var readDb = _fixture.CreateContext();
            var service = new TransactionService(readDb, TestAudit.For(readDb));

            var outgoing = (await service.GetHistoryAsync(a)).First();
            var incoming = (await service.GetHistoryAsync(b)).First();

            Assert.Equal(-1000m, outgoing.Amount);
            Assert.Equal(1000m, incoming.Amount);
        }

        [Fact]
        public async Task TransferredMoney_CanBeWithdrawn()
        {
            // Guards the other manual-testing bug: the withdrawal balance
            // check treated TransferIn as money going out, so B looked
            // empty (or negative) and this withdrawal was refused.
            var (_, a, b) = await TestData.CreateCustomerWithAccountsAsync(_fixture, 10000m);

            await using var db = _fixture.CreateContext();
            var service = new TransactionService(db, TestAudit.For(db));

            var transfer = await service.TransferAsync(a, b, 3000m);
            Assert.True(transfer.Success, transfer.Error);

            var withdrawal = await service.WithdrawAsync(b, 3000m);
            Assert.True(withdrawal.Success, withdrawal.Error);
            Assert.Equal(0m, await BalanceAsync(b));
        }

        [Fact]
        public async Task Transfer_ToAnotherCustomersAccount_IsRefused_AndNothingMoves()
        {
            // Calls the service directly, skipping the controller's ownership
            // check on purpose. This proves the service enforces "own
            // accounts only" by itself -- the safety net if a future caller
            // forgets the controller check.
            var (_, myAccount, _) = await TestData.CreateCustomerWithAccountsAsync(_fixture, 10000m);
            var (_, theirAccount, _) = await TestData.CreateCustomerWithAccountsAsync(_fixture, 0m);

            await using var db = _fixture.CreateContext();
            var result = await new TransactionService(db, TestAudit.For(db)).TransferAsync(myAccount, theirAccount, 1000m);

            Assert.False(result.Success);
            Assert.Equal("Kontot kunde inte hittas.", result.Error);
            Assert.Equal(10000m, await BalanceAsync(myAccount));
            Assert.Equal(0m, await BalanceAsync(theirAccount));
        }

        [Fact]
        public async Task Transfer_WithInsufficientFunds_WritesNothing()
        {
            var (_, a, b) = await TestData.CreateCustomerWithAccountsAsync(_fixture, 500m);
            var rowsOnABefore = await TransactionCountAsync(a);
            var rowsOnBBefore = await TransactionCountAsync(b);

            await using var db = _fixture.CreateContext();
            var result = await new TransactionService(db, TestAudit.For(db)).TransferAsync(a, b, 1000m);

            Assert.False(result.Success);
            Assert.Equal("Otillräckligt saldo.", result.Error);

            // Not just "balance unchanged" -- no half-transfer rows at all.
            Assert.Equal(rowsOnABefore, await TransactionCountAsync(a));
            Assert.Equal(rowsOnBBefore, await TransactionCountAsync(b));
        }

        [Fact]
        public async Task ConcurrentTransfers_ExceedingBalance_OnlyOneSucceeds()
        {
            // 10000 on A. Two transfers of 7000 are each valid alone but
            // together would overdraw A -- the same race the withdrawal
            // concurrency test covers, now for transfers.
            var (_, a, b) = await TestData.CreateCustomerWithAccountsAsync(_fixture, 10000m);

            // Separate contexts = two independent requests, as in production.
            await using var db1 = _fixture.CreateContext();
            await using var db2 = _fixture.CreateContext();

            var results = await Task.WhenAll(
                new TransactionService(db1, TestAudit.For(db1)).TransferAsync(a, b, 7000m),
                new TransactionService(db2, TestAudit.For(db2)).TransferAsync(a, b, 7000m));

            Assert.Equal(1, results.Count(r => r.Success));

            // The loser can fail two ways depending on exact timing: Postgres
            // detects the overlap ("samtidig åtkomst"), or the second one runs
            // after the first committed and sees too little money. Both are
            // correct outcomes; the balances below are the real guarantee.
            var failure = results.Single(r => !r.Success);
            Assert.True(
                failure.Error!.Contains("samtidig åtkomst") || failure.Error == "Otillräckligt saldo.",
                $"Unexpected failure message: {failure.Error}");

            Assert.Equal(3000m, await BalanceAsync(a));
            Assert.Equal(7000m, await BalanceAsync(b));
        }
    }
}