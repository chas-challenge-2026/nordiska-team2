using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NordiskaPortal.Api.Data;
using NordiskaPortal.Api.Models;
using NordiskaPortal.Api.Services;
using Xunit;

namespace NordiskaPortal.Api.Tests;

public class NotificationServiceTests
{
    private static BankContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<BankContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new BankContext(options);
    }

    private static async Task SeedAccountAsync(BankContext db, int id, string email, string accountNumber)
    {
        var customer = new Customer
        {
            Id = id,
            Name = $"Kund {id}",
            Email = email,
            PasswordHash = "hash",
            PersonalId = $"19850505-{1000 + id}"
        };
        db.Customers.Add(customer);
        db.SavingsAccounts.Add(new SavingsAccount
        {
            Id = id,
            CustomerId = id,
            AccountNumber = accountNumber,
            Customer = customer
        });
        await db.SaveChangesAsync();
    }

    private static TransactionService CreateTransactionService(BankContext db) =>
        new(db,
            TestAudit.For(db),
            new NotificationService(db, NullLogger<NotificationService>.Instance),
            NullLogger<TransactionService>.Instance);

    // A worker whose "email provider" always fails, to exercise retry/backoff.
    private sealed class FailingWorker : NotificationBackgroundWorker
    {
        public FailingWorker(IServiceScopeFactory scopeFactory)
            : base(scopeFactory, NullLogger<NotificationBackgroundWorker>.Instance) { }

        protected override Task DispatchEmailAsync(string recipient, string type, string message, CancellationToken cancellationToken)
            => throw new InvalidOperationException("SMTP down");
    }

    private static (IServiceProvider Provider, string DbName) CreateWorkerProvider()
    {
        var services = new ServiceCollection();
        var dbName = Guid.NewGuid().ToString();
        services.AddDbContext<BankContext>(options => options.UseInMemoryDatabase(dbName));
        return (services.BuildServiceProvider(), dbName);
    }

    private static async Task AddNotificationAsync(IServiceProvider provider, string refId, Action<Notification>? configure = null)
    {
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BankContext>();
        var n = new Notification
        {
            CustomerId = 1,
            Recipient = "anna@example.com",
            Type = "deposit_confirmation",
            Message = "Testmeddelande",
            RefId = refId,
            Status = "pending",
            CreatedAt = DateTime.UtcNow
        };
        configure?.Invoke(n);
        db.Notifications.Add(n);
        await db.SaveChangesAsync();
    }

    private static async Task<Notification> LoadNotificationAsync(IServiceProvider provider, string refId)
    {
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BankContext>();
        return await db.Notifications.AsNoTracking().FirstAsync(n => n.RefId == refId);
    }

    // ---------- NotificationService ----------

    [Fact]
    public async Task StageTransactionNotificationAsync_StagesPendingRow_ButDoesNotSaveIt()
    {
        using var db = CreateInMemoryDb();
        var service = new NotificationService(db, NullLogger<NotificationService>.Instance);

        await service.StageTransactionNotificationAsync(
            customerId: 1,
            recipient: "anna@example.com",
            type: "deposit_confirmation",
            refId: "tx-123",
            message: "Insättning på 1 000,00 kr bekräftad.");

        // Staged on the context but not persisted until the caller saves.
        Assert.Equal(EntityState.Added, db.ChangeTracker.Entries<Notification>().Single().State);

        await db.SaveChangesAsync();

        var notification = await db.Notifications.FirstOrDefaultAsync(n => n.RefId == "tx-123");
        Assert.NotNull(notification);
        Assert.Equal(1, notification.CustomerId);
        Assert.Equal("anna@example.com", notification.Recipient);
        Assert.Equal("deposit_confirmation", notification.Type);
        Assert.Equal("pending", notification.Status);
        Assert.Equal(0, notification.RetryCount);
        Assert.Null(notification.SentAt);
        Assert.Null(notification.NextAttemptAt);
    }

    [Fact]
    public async Task StageTransactionNotificationAsync_WithDuplicateRefId_IsIdempotent_EvenBeforeSaving()
    {
        using var db = CreateInMemoryDb();
        var service = new NotificationService(db, NullLogger<NotificationService>.Instance);

        await service.StageTransactionNotificationAsync(1, "anna@example.com", "deposit_confirmation", "tx-999", "Första");
        await service.StageTransactionNotificationAsync(1, "anna@example.com", "deposit_confirmation", "tx-999", "Andra (dubblett)");
        await db.SaveChangesAsync();
        await service.StageTransactionNotificationAsync(1, "anna@example.com", "deposit_confirmation", "tx-999", "Tredje (dubblett)");
        await db.SaveChangesAsync();

        Assert.Equal(1, await db.Notifications.CountAsync(n => n.RefId == "tx-999"));
    }

    [Fact]
    public async Task GetNotificationsForCustomerAsync_ShouldReturnOnlyCustomerNotificationsInDescendingOrder()
    {
        using var db = CreateInMemoryDb();
        var service = new NotificationService(db, NullLogger<NotificationService>.Instance);

        var now = DateTime.UtcNow;

        db.Notifications.AddRange(
            new Notification
            {
                CustomerId = 1,
                Recipient = "anna@example.com",
                Type = "deposit_confirmation",
                Message = "Tidig notis",
                RefId = "tx-1",
                Status = "sent",
                CreatedAt = now.AddMinutes(-10),
                SentAt = now.AddMinutes(-9)
            },
            new Notification
            {
                CustomerId = 1,
                Recipient = "anna@example.com",
                Type = "withdrawal_confirmation",
                Message = "Sen notis",
                RefId = "tx-2",
                Status = "pending",
                CreatedAt = now,
                SentAt = null
            },
            new Notification
            {
                CustomerId = 2, // Annan kund
                Recipient = "erik@example.com",
                Type = "deposit_confirmation",
                Message = "Eriks notis",
                RefId = "tx-3",
                Status = "pending",
                CreatedAt = now
            }
        );
        await db.SaveChangesAsync();

        var result = await service.GetNotificationsForCustomerAsync(1);

        Assert.Equal(2, result.Count);
        Assert.Equal("Sen notis", result[0].Message);
        Assert.Equal("Tidig notis", result[1].Message);
        Assert.DoesNotContain(result, n => n.Message == "Eriks notis");
    }

    // ---------- TransactionService integration ----------

    [Fact]
    public async Task TransactionService_DepositAsync_WritesNotificationWithTransactionIdAsRef()
    {
        using var db = CreateInMemoryDb();
        await SeedAccountAsync(db, 1, "anna@example.com", "NKM-10001");

        var result = await CreateTransactionService(db).DepositAsync(1, 2500m);

        Assert.True(result.Success);

        var transaction = await db.Transactions.SingleAsync();
        var notification = await db.Notifications.SingleAsync();
        Assert.Equal(1, notification.CustomerId);
        Assert.Equal("deposit_confirmation", notification.Type);
        Assert.Equal("pending", notification.Status);
        Assert.Equal("anna@example.com", notification.Recipient);
        Assert.Equal(transaction.Id.ToString(), notification.RefId);
        Assert.Contains("Insättning på", notification.Message);
        Assert.Contains("NKM-10001", notification.Message);
    }

    [Fact]
    public async Task TransactionService_WithdrawAsync_WritesNotificationWithTransactionIdAsRef()
    {
        using var db = CreateInMemoryDb();
        await SeedAccountAsync(db, 2, "erik@example.com", "NKM-20001");
        var service = CreateTransactionService(db);

        await service.DepositAsync(2, 1000m);
        db.Notifications.RemoveRange(db.Notifications);
        await db.SaveChangesAsync();

        var withdrawResult = await service.WithdrawAsync(2, 400m);

        Assert.True(withdrawResult.Success);

        var withdrawal = await db.Transactions.SingleAsync(t => t.Type == TransactionType.Withdrawal);
        var notification = await db.Notifications.SingleAsync();
        Assert.Equal("withdrawal_confirmation", notification.Type);
        Assert.Equal("erik@example.com", notification.Recipient);
        Assert.Equal(withdrawal.Id.ToString(), notification.RefId);
        Assert.Contains("Uttag på", notification.Message);
        Assert.Contains("NKM-20001", notification.Message);
    }

    [Fact]
    public async Task TransactionService_WithdrawAsync_WithInsufficientFunds_WritesNoNotification()
    {
        using var db = CreateInMemoryDb();
        await SeedAccountAsync(db, 2, "erik@example.com", "NKM-20001");

        var result = await CreateTransactionService(db).WithdrawAsync(2, 400m);

        Assert.False(result.Success);
        Assert.Empty(db.Notifications);
    }

    [Fact]
    public async Task TransactionService_Deposit_ForCustomerWithoutEmail_StillSucceeds_WithoutInventingARecipient()
    {
        using var db = CreateInMemoryDb();
        await SeedAccountAsync(db, 3, "", "NKM-30001");

        var result = await CreateTransactionService(db).DepositAsync(3, 100m);

        Assert.True(result.Success);
        Assert.Single(db.Transactions);
        Assert.Empty(db.Notifications);
    }

    [Fact]
    public async Task TransactionService_Deposit_AlsoWritesTheAuditEntry()
    {
        using var db = CreateInMemoryDb();
        await SeedAccountAsync(db, 1, "anna@example.com", "NKM-10001");

        await CreateTransactionService(db).DepositAsync(1, 100m);

        Assert.Single(db.AuditEntries);
        Assert.Single(db.Notifications);
    }

    // ---------- NotificationBackgroundWorker ----------

    [Fact]
    public async Task NotificationBackgroundWorker_ProcessPendingNotificationsAsync_ShouldMarkAsSent()
    {
        var (provider, _) = CreateWorkerProvider();
        await AddNotificationAsync(provider, "tx-test-1");

        var worker = new NotificationBackgroundWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<NotificationBackgroundWorker>.Instance);

        int processed = await worker.ProcessPendingNotificationsAsync(CancellationToken.None);

        Assert.Equal(1, processed);
        var notification = await LoadNotificationAsync(provider, "tx-test-1");
        Assert.Equal("sent", notification.Status);
        Assert.NotNull(notification.SentAt);
    }

    [Fact]
    public async Task NotificationBackgroundWorker_OnFailure_SchedulesRetryWithBackoff_AndDoesNotRetryBeforeItIsDue()
    {
        var (provider, _) = CreateWorkerProvider();
        await AddNotificationAsync(provider, "tx-fail-1");
        var worker = new FailingWorker(provider.GetRequiredService<IServiceScopeFactory>());

        var before = DateTime.UtcNow;
        Assert.Equal(1, await worker.ProcessPendingNotificationsAsync());

        var afterFirst = await LoadNotificationAsync(provider, "tx-fail-1");
        Assert.Equal("pending", afterFirst.Status);
        Assert.Equal(1, afterFirst.RetryCount);
        Assert.NotNull(afterFirst.NextAttemptAt);
        Assert.True(afterFirst.NextAttemptAt >= before + NotificationBackgroundWorker.BackoffFor(1) - TimeSpan.FromSeconds(1));

        // Polling again immediately must NOT pick it up: its backoff has not elapsed.
        Assert.Equal(0, await worker.ProcessPendingNotificationsAsync());
        Assert.Equal(1, (await LoadNotificationAsync(provider, "tx-fail-1")).RetryCount);
    }

    [Fact]
    public async Task NotificationBackgroundWorker_AfterMaxRetries_MarksNotificationAsFailed()
    {
        var (provider, _) = CreateWorkerProvider();
        // Already failed twice and due now: the next failure is the last allowed attempt.
        await AddNotificationAsync(provider, "tx-fail-2", n =>
        {
            n.RetryCount = NotificationBackgroundWorker.MaxRetries - 1;
            n.NextAttemptAt = DateTime.UtcNow.AddSeconds(-1);
        });
        var worker = new FailingWorker(provider.GetRequiredService<IServiceScopeFactory>());

        Assert.Equal(1, await worker.ProcessPendingNotificationsAsync());

        var notification = await LoadNotificationAsync(provider, "tx-fail-2");
        Assert.Equal("failed", notification.Status);
        Assert.Equal(NotificationBackgroundWorker.MaxRetries, notification.RetryCount);
        Assert.Null(notification.NextAttemptAt);
        Assert.Equal(0, await worker.ProcessPendingNotificationsAsync());
    }

    [Fact]
    public async Task NotificationBackgroundWorker_SkipsNotificationsWhoseRetryIsNotDueYet()
    {
        var (provider, _) = CreateWorkerProvider();
        await AddNotificationAsync(provider, "tx-later", n =>
        {
            n.RetryCount = 1;
            n.NextAttemptAt = DateTime.UtcNow.AddMinutes(10);
        });
        var worker = new NotificationBackgroundWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<NotificationBackgroundWorker>.Instance);

        Assert.Equal(0, await worker.ProcessPendingNotificationsAsync());
        Assert.Equal("pending", (await LoadNotificationAsync(provider, "tx-later")).Status);
    }

    [Fact]
    public void BackoffFor_GrowsWithEachRetry()
    {
        Assert.Equal(TimeSpan.FromSeconds(30), NotificationBackgroundWorker.BackoffFor(1));
        Assert.Equal(TimeSpan.FromMinutes(2), NotificationBackgroundWorker.BackoffFor(2));
        Assert.True(NotificationBackgroundWorker.BackoffFor(3) > NotificationBackgroundWorker.BackoffFor(2));
    }
}
