using NordiskaPortal.Api.Services;
using Xunit;

namespace NordiskaPortal.Api.Tests
{
    [Collection("Postgres collection")]
    public class TransactionServiceConcurrencyTests
    {
        private readonly PostgresFixture _fixture;

        public TransactionServiceConcurrencyTests(PostgresFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task ConcurrentWithdrawals_ExceedingCombinedBalance_OnlyOneSucceeds()
        {
            // Arrange: account 1 (Anna's NKM-10001) is seeded with a single
            // 125000 deposit via the real migration's HasData. Two withdrawals
            // of 70000 each are individually valid (70000 < 125000) but
            // together (140000) would overdraw the account.
            const int accountId = 1;
            const decimal withdrawAmount = 70000m;

            // Two separate DbContext instances and two separate TransactionService
            // instances — this mirrors two real, independent HTTP requests, each
            // with their own scoped DbContext, rather than one shared context.
            await using var db1 = _fixture.CreateContext();
            await using var db2 = _fixture.CreateContext();
            var service1 = new TransactionService(db1);
            var service2 = new TransactionService(db2);

            // Act: fire both withdrawals genuinely concurrently. Task.WhenAll
            // starts both before awaiting either, so both begin their balance
            // read before either has committed — the exact scenario the
            // Serializable isolation exists to protect against.
            var task1 = service1.WithdrawAsync(accountId, withdrawAmount);
            var task2 = service2.WithdrawAsync(accountId, withdrawAmount);

            var results = await Task.WhenAll(task1, task2);

            // Assert: exactly one succeeded, exactly one failed.
            var successCount = results.Count(r => r.Success);
            var failureCount = results.Count(r => !r.Success);

            Assert.Equal(1, successCount);
            Assert.Equal(1, failureCount);

            // The failure should be the specific concurrency-conflict message,
            // not "Otillräckligt saldo" — that distinction is what proves the
            // Serializable isolation caught a genuine overlapping conflict,
            // rather than the second request simply seeing an already-reduced
            // balance from a sequential (non-overlapping) execution.
            var failure = results.First(r => !r.Success);
            Assert.Contains("samtidig åtkomst", failure.Error);

            // Regardless of which specific message won the race, the balance
            // itself must reflect exactly one withdrawal — this is the actual
            // correctness guarantee, independent of the message assertion above.
            await using var verifyDb = _fixture.CreateContext();
            var verifyService = new TransactionService(verifyDb);
            var finalBalance = await verifyService.GetBalanceAsync(accountId);

            Assert.Equal(125000m - withdrawAmount, finalBalance);
        }

        [Fact]
        public async Task ConcurrentDeposits_BothSucceed_NoRaceCondition()
        {
            // Deposits should NOT race with each other at all — this test
            // confirms that two concurrent deposits both succeed cleanly,
            // contrasting with the withdrawal test above where one is
            // expected to fail. Uses account 2 (also seeded, 45000 opening
            // balance) to stay independent of the withdrawal test's account.
            const int accountId = 2;
            const decimal depositAmount = 1000m;

            await using var db1 = _fixture.CreateContext();
            await using var db2 = _fixture.CreateContext();
            var service1 = new TransactionService(db1);
            var service2 = new TransactionService(db2);

            var task1 = service1.DepositAsync(accountId, depositAmount);
            var task2 = service2.DepositAsync(accountId, depositAmount);

            var results = await Task.WhenAll(task1, task2);

            Assert.All(results, r => Assert.True(r.Success));

            await using var verifyDb = _fixture.CreateContext();
            var verifyService = new TransactionService(verifyDb);
            var finalBalance = await verifyService.GetBalanceAsync(accountId);

            Assert.Equal(45000m + depositAmount + depositAmount, finalBalance);
        }
    }
}