using Microsoft.EntityFrameworkCore;
using NordiskaPortal.Api.Data;
using NordiskaPortal.Api.Services.BankId;

// REAL BankID (the test environment, given the test certificate and BaseUrl).
// Same contract as MockBankIdService, so BankIdController and the frontend don't change.
namespace NordiskaPortal.Api.Services
{
    public class RealBankIdService : IBankIdService
    {
        // BankID wants collect called about every 2 seconds. The frontend polls every second
        // for the QR code, so in between we answer from the last collect result.
        private static readonly TimeSpan CollectInterval = TimeSpan.FromSeconds(2);

        private readonly IBankIdApiClient _api;
        private readonly BankIdOrderStore _store;
        private readonly TimeProvider _time;
        private readonly BankContext _db;
        private readonly IAuthService _authService;
        private readonly ILogger<RealBankIdService> _logger;

        public RealBankIdService(IBankIdApiClient api, BankIdOrderStore store, TimeProvider time,
                                 BankContext db, IAuthService authService, ILogger<RealBankIdService> logger)
        {
            _api = api;
            _store = store;
            _time = time;
            _db = db;
            _authService = authService;
            _logger = logger;
        }

        public async Task<BankIdStartResult> StartAsync(string endUserIp, CancellationToken ct = default)
        {
            var auth = await _api.AuthAsync(endUserIp, ct);
            // The QR time counts from when BankID's response arrived, so stamp it now.
            _store.Add(new BankIdOrder(auth.OrderRef, auth.AutoStartToken, auth.QrStartToken,
                                       auth.QrStartSecret, _time.GetUtcNow()));
            return new BankIdStartResult(auth.OrderRef, auth.AutoStartToken);
        }

        public async Task<BankIdStatusResult> GetStatusAsync(string orderRef, CancellationToken ct = default)
        {
            var order = _store.Get(orderRef);
            if (order is null)
                return BankIdStatusResult.Failed(null);

            var now = _time.GetUtcNow();
            var qrData = BankIdQrCode.Generate(order.QrStartToken, order.QrStartSecret, order.CreatedAt, now);

            // Asked BankID less than 2 seconds ago: answer from that result, with a fresh QR code.
            if (order.LastCollectedAt is { } last && now - last < CollectInterval)
                return BankIdStatusResult.Pending(order.LastHintCode ?? BankIdHintCodes.OutstandingTransaction, qrData);

            BankIdCollectResponse collect;
            try
            {
                collect = await _api.CollectAsync(orderRef, ct);
            }
            catch (BankIdApiException ex)
            {
                _logger.LogWarning(ex, "BankID collect failed for order {OrderRef}", orderRef);
                _store.TryRemove(orderRef);
                return BankIdStatusResult.Failed(null);
            }

            switch (collect.Status)
            {
                case "pending":
                    var hint = collect.HintCode ?? BankIdHintCodes.OutstandingTransaction;
                    _store.TryReplace(order, order with { LastCollectedAt = now, LastHintCode = hint });
                    return BankIdStatusResult.Pending(hint, qrData);

                case "complete":
                    // Only the caller that removes the order gets tokens: one-time use.
                    if (!_store.TryRemove(orderRef))
                        return BankIdStatusResult.Failed(null);
                    return await CompleteLoginAsync(collect, ct);

                default: // "failed"
                    _store.TryRemove(orderRef);
                    return BankIdStatusResult.Failed(collect.HintCode);
            }
        }

        public async Task CancelAsync(string orderRef, CancellationToken ct = default)
        {
            if (!_store.TryRemove(orderRef))
                return;
            try
            {
                await _api.CancelAsync(orderRef, ct);
            }
            catch (BankIdApiException ex)
            {
                _logger.LogInformation(ex, "BankID cancel for {OrderRef} failed; it may already be finished", orderRef);
            }
        }

        private async Task<BankIdStatusResult> CompleteLoginAsync(BankIdCollectResponse collect, CancellationToken ct)
        {
            var personalNumber = collect.CompletionData?.User.PersonalNumber;
            if (string.IsNullOrEmpty(personalNumber))
                return BankIdStatusResult.Failed(null);

            var formats = PersonalNumberFormats(personalNumber);
            var customer = await _db.Customers.FirstOrDefaultAsync(c => formats.Contains(c.PersonalId), ct);
            if (customer is null)
                return BankIdStatusResult.Failed(BankIdHintCodes.NotACustomer);

            // Same token path as password login.
            var tokens = await _authService.IssueTokensForCustomerAsync(customer.Id, customer.Email);
            return BankIdStatusResult.Complete(tokens.AccessToken, tokens.RefreshToken);
        }

        // BankID returns 12 digits ("198505051234"); the seed data uses "19850505-1234". Match both.
        private static string[] PersonalNumberFormats(string personalNumber)
        {
            var digits = new string(personalNumber.Where(char.IsDigit).ToArray());
            return digits.Length == 12 ? [digits, digits.Insert(8, "-")] : [digits];
        }
    }
}