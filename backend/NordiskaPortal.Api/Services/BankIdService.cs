using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using NordiskaPortal.Api.Data;

// MOCK BankID
// Simulates the real start-then-poll shape, order created -> caller polls status until it resolves.
// Not including any real BankID infrastructure, certificates, or personal numbers.
//
// Registered as a singleton because it must remember pending orders across multiple separate HTTP requests: one /start call, then several /status polls. 
// BankContext and IAuthService are both Scoped (per-request).
// A Singleton constructor-injecting a Scoped service directly is a classic ASP.NET Core bug (a "captive dependency"): 
// The Scoped instance gets trapped for the Singleton's entire  lifetime, and every request after the first gets a disposed, broken DbContext. 
// 
// Fix: IServiceScopeFactory, it lets this Singleton create a fresh, valid scope only at the moment it actually needs database access.
namespace NordiskaPortal.Api.Services
{
    internal record BankIdMockOrder(string PersonalId, DateTime CreatedAt);

    public class BankIdService : IBankIdService
    {
        // Simulates the real-world delay of a person picking up their phone and approving the order. 
        // Deliberately not instant, as instant completion would make the async/polling shape invisible 
        // in a demo, which is the whole point of building this as start+poll rather than a single call.
        private static readonly TimeSpan SimulatedApprovalDelay = TimeSpan.FromSeconds(3);

        private readonly ConcurrentDictionary<string, BankIdMockOrder> _orders = new();
        private readonly IServiceScopeFactory _scopeFactory;

        public BankIdService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public string StartOrder(string personalId)
        {
            var orderRef = Guid.NewGuid().ToString();
            _orders[orderRef] = new BankIdMockOrder(personalId, DateTime.UtcNow);
            return orderRef;
        }

        public async Task<BankIdStatusResult> GetStatusAsync(string orderRef)
        {
            if (!_orders.TryGetValue(orderRef, out var order))
                return new BankIdStatusResult("failed", null, null);

            if (DateTime.UtcNow - order.CreatedAt < SimulatedApprovalDelay)
                return new BankIdStatusResult("pending", null, null);

            // "Approved", one-time use, remove regardless of outcome below so a completed/failed order can't be polled again.
            _orders.TryRemove(orderRef, out _);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BankContext>();
            var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

            var customer = await db.Customers.FirstOrDefaultAsync(c => c.PersonalId == order.PersonalId);
            if (customer == null)
                return new BankIdStatusResult("failed", null, null);

            // Same issuance path as password login.
            // A BankID-confirmed session is indistinguishable from a password-confirmed one from here on. 
            // Nothing downstream (refresh, logout, [Authorize], ownership checks) needs to know or care which one happened.
            var tokens = await authService.IssueTokensForCustomerAsync(customer.Id, customer.Email);
            return new BankIdStatusResult("complete", tokens.AccessToken, tokens.RefreshToken);
        }
    }
}