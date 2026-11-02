namespace NordiskaPortal.Api.Services
{
    public record BankIdStartResult(string OrderRef, string AutoStartToken);

    // Status is "pending", "complete" or "failed", the same words BankID's collect uses.
    public record BankIdStatusResult(
        string Status, string? HintCode, string? QrData, string? AccessToken, string? RefreshToken)
    {
        public static BankIdStatusResult Pending(string hintCode, string? qrData) =>
            new("pending", hintCode, qrData, null, null);

        public static BankIdStatusResult Failed(string? hintCode) =>
            new("failed", hintCode, null, null, null);

        public static BankIdStatusResult Complete(string accessToken, string refreshToken) =>
            new("complete", null, null, accessToken, refreshToken);
    }

    public static class BankIdHintCodes
    {
        // BankID's own codes
        public const string OutstandingTransaction = "outstandingTransaction";
        public const string StartFailed = "startFailed";

        // Ours, not BankID's: identity confirmed, but no customer has that personal number.
        public const string NotACustomer = "notACustomer";
    }

    public interface IBankIdService
    {
        Task<BankIdStartResult> StartAsync(string endUserIp, CancellationToken ct = default);
        Task<BankIdStatusResult> GetStatusAsync(string orderRef, CancellationToken ct = default);
        Task CancelAsync(string orderRef, CancellationToken ct = default);
    }
}