using Microsoft.EntityFrameworkCore;
using NordiskaPortal.Api.Data;
using NordiskaPortal.Api.Services.BankId;

// MOCK BankID, shaped like the real v6 flow:
// - start takes no personal number; identity is only known when the order completes
// - while pending, status returns an animated QR string (a new one every second)
// - the order fails with startFailed if nobody "scans" within 30 seconds, like the real service
// - the user's phone is simulated by Approve(), called from a dev-only endpoint
//
// Order state lives in BankIdOrderStore (singleton), so this service is an ordinary scoped
// service with BankContext and IAuthService injected directly. That replaces the
// IServiceScopeFactory workaround the old singleton version needed.
namespace NordiskaPortal.Api.Services
{
    public class MockBankIdService : IBankIdService
    {
        public static readonly TimeSpan QrScanWindow = TimeSpan.FromSeconds(30);

        private readonly BankIdOrderStore _store;
        private readonly TimeProvider _time;
        private readonly BankContext _db;
        private readonly IAuthService _authService;

        public MockBankIdService(BankIdOrderStore store, TimeProvider time,
                                 BankContext db, IAuthService authService)
        {
            _store = store;
            _time = time;
            _db = db;
            _authService = authService;
        }

        public Task<BankIdStartResult> StartAsync(string endUserIp, CancellationToken ct = default)
        {
            // endUserIp is unused here; the real service must send it to BankID.
            var order = new BankIdOrder(
                OrderRef: Guid.NewGuid().ToString(),
                AutoStartToken: Guid.NewGuid().ToString(),
                QrStartToken: Guid.NewGuid().ToString(),
                QrStartSecret: Guid.NewGuid().ToString(),
                CreatedAt: _time.GetUtcNow());

            _store.Add(order);
            return Task.FromResult(new BankIdStartResult(order.OrderRef, order.AutoStartToken));
        }

        public async Task<BankIdStatusResult> GetStatusAsync(string orderRef, CancellationToken ct = default)
        {
            var order = _store.Get(orderRef);
            if (order is null)
                return BankIdStatusResult.Failed(null);

            var now = _time.GetUtcNow();

            if (order.ApprovedPersonalId is null)
            {
                if (now - order.CreatedAt >= QrScanWindow)
                {
                    _store.TryRemove(orderRef);
                    return BankIdStatusResult.Failed(BankIdHintCodes.StartFailed);
                }

                var qrData = BankIdQrCode.Generate(order.QrStartToken, order.QrStartSecret, order.CreatedAt, now);
                return BankIdStatusResult.Pending(BankIdHintCodes.OutstandingTransaction, qrData);
            }

            // Approved. Only the caller that actually removes the order gets tokens: one-time use.
            if (!_store.TryRemove(orderRef))
                return BankIdStatusResult.Failed(null);

            var customer = await _db.Customers
                .FirstOrDefaultAsync(c => c.PersonalId == order.ApprovedPersonalId, ct);
            if (customer is null)
                return BankIdStatusResult.Failed(BankIdHintCodes.NotACustomer);

            // Same issuance path as password login, so nothing downstream needs to know which one happened.
            var tokens = await _authService.IssueTokensForCustomerAsync(customer.Id, customer.Email);
            return BankIdStatusResult.Complete(tokens.AccessToken, tokens.RefreshToken);
        }

        public Task CancelAsync(string orderRef, CancellationToken ct = default)
        {
            _store.TryRemove(orderRef);
            return Task.CompletedTask;
        }

        // Stands in for the user scanning the QR code and approving on their phone.
        public bool Approve(string orderRef, string personalId)
        {
            var order = _store.Get(orderRef);
            if (order is null || order.ApprovedPersonalId is not null)
                return false;

            if (_time.GetUtcNow() - order.CreatedAt >= QrScanWindow)
                return false;

            return _store.TryReplace(order, order with { ApprovedPersonalId = personalId });
        }
    }
}