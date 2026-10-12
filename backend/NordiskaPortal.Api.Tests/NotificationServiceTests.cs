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

    [Fact]
    public async Task EnqueueTransactionNotificationAsync_ShouldCreatePendingNotificationInDatabase()
    {
        using var db = CreateInMemoryDb();
        var service = new NotificationService(db, NullLogger<NotificationService>.Instance);

        await service.EnqueueTransactionNotificationAsync(
            customerId: 1,
            recipient: "anna@example.com",
            type: "deposit_confirmation",
            refId: "tx-123",
            message: "Insättning på 1 000,00 kr bekräftad."
        );

        var notification = await db.Notifications.FirstOrDefaultAsync(n => n.RefId == "tx-123");
        Assert.NotNull(notification);
        Assert.Equal(1, notification.CustomerId);
        Assert.Equal("anna@example.com", notification.Recipient);
        Assert.Equal("deposit_confirmation", notification.Type);
        Assert.Equal("Insättning på 1 000,00 kr bekräftad.", notification.Message);
        Assert.Equal("pending", notification.Status);
        Assert.Equal(0, notification.RetryCount);
        Assert.Null(notification.SentAt);
    }

    [Fact]
    public async Task EnqueueTransactionNotificationAsync_WithDuplicateRefId_ShouldBeIdempotent()
    {
        using var db = CreateInMemoryDb();
        var service = new NotificationService(db, NullLogger<NotificationService>.Instance);

        await service.EnqueueTransactionNotificationAsync(1, "anna@example.com", "deposit_confirmation", "tx-999", "Första");
        await service.EnqueueTransactionNotificationAsync(1, "anna@example.com", "deposit_confirmation", "tx-999", "Andra (dubblett)");

        var count = await db.Notifications.CountAsync(n => n.RefId == "tx-999");
        Assert.Equal(1, count);
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

    [Fact]
    public async Task TransactionService_DepositAsync_ShouldEnqueueNotification()
    {
        using var db = CreateInMemoryDb();
        var customer = new Customer
        {
            Id = 1,
            Name = "Anna Lindqvist",
            Email = "anna@example.com",
            PasswordHash = "hash",
            PersonalId = "19850505-1234"
        };
        var account = new SavingsAccount
        {
            Id = 1,
            CustomerId = 1,
            AccountNumber = "NKM-10001",
            Customer = customer
        };
        db.Customers.Add(customer);
        db.SavingsAccounts.Add(account);
        await db.SaveChangesAsync();

        var notificationService = new NotificationService(db, NullLogger<NotificationService>.Instance);
        var transactionService = new TransactionService(db, notificationService, NullLogger<TransactionService>.Instance);

        var result = await transactionService.DepositAsync(1, 2500m);

        Assert.True(result.Success);

        var notification = await db.Notifications.FirstOrDefaultAsync(n => n.CustomerId == 1);
        Assert.NotNull(notification);
        Assert.Equal("deposit_confirmation", notification.Type);
        Assert.Equal("pending", notification.Status);
        Assert.Equal("anna@example.com", notification.Recipient);
        Assert.Contains("Insättning på", notification.Message);
        Assert.Contains("NKM-10001", notification.Message);
    }

    [Fact]
    public async Task TransactionService_WithdrawAsync_ShouldEnqueueNotification()
    {
        using var db = CreateInMemoryDb();
        var customer = new Customer
        {
            Id = 2,
            Name = "Erik Johansson",
            Email = "erik@example.com",
            PasswordHash = "hash",
            PersonalId = "19900101-5678"
        };
        var account = new SavingsAccount
        {
            Id = 2,
            CustomerId = 2,
            AccountNumber = "NKM-20001",
            Customer = customer
        };
        db.Customers.Add(customer);
        db.SavingsAccounts.Add(account);
        await db.SaveChangesAsync();

        var notificationService = new NotificationService(db, NullLogger<NotificationService>.Instance);
        var transactionService = new TransactionService(db, notificationService, NullLogger<TransactionService>.Instance);

        // First deposit 1000 kr so we have balance
        await transactionService.DepositAsync(2, 1000m);

        // Clear notification from deposit so we can test withdrawal notification cleanly
        db.Notifications.RemoveRange(db.Notifications);
        await db.SaveChangesAsync();

        var withdrawResult = await transactionService.WithdrawAsync(2, 400m);

        Assert.True(withdrawResult.Success);

        var notification = await db.Notifications.FirstOrDefaultAsync(n => n.CustomerId == 2);
        Assert.NotNull(notification);
        Assert.Equal("withdrawal_confirmation", notification.Type);
        Assert.Equal("pending", notification.Status);
        Assert.Equal("erik@example.com", notification.Recipient);
        Assert.Contains("Uttag på", notification.Message);
        Assert.Contains("NKM-20001", notification.Message);
    }

    [Fact]
    public async Task NotificationBackgroundWorker_ProcessPendingNotificationsAsync_ShouldMarkAsSent()
    {
        var services = new ServiceCollection();
        var dbName = Guid.NewGuid().ToString();
        services.AddDbContext<BankContext>(options => options.UseInMemoryDatabase(dbName));
        var serviceProvider = services.BuildServiceProvider();

        using (var setupScope = serviceProvider.CreateScope())
        {
            var db = setupScope.ServiceProvider.GetRequiredService<BankContext>();
            db.Notifications.Add(new Notification
            {
                CustomerId = 1,
                Recipient = "anna@example.com",
                Type = "deposit_confirmation",
                Message = "Testmeddelande",
                RefId = "tx-test-1",
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var worker = new NotificationBackgroundWorker(scopeFactory, NullLogger<NotificationBackgroundWorker>.Instance);

        int processed = await worker.ProcessPendingNotificationsAsync(CancellationToken.None);

        Assert.Equal(1, processed);

        using (var verifyScope = serviceProvider.CreateScope())
        {
            var db = verifyScope.ServiceProvider.GetRequiredService<BankContext>();
            var notification = await db.Notifications.FirstAsync(n => n.RefId == "tx-test-1");
            Assert.Equal("sent", notification.Status);
            Assert.NotNull(notification.SentAt);
        }
    }
}
