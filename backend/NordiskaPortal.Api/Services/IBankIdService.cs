namespace NordiskaPortal.Api.Services
{
    public record BankIdStatusResult(string Status, string? AccessToken, string? RefreshToken);

    public interface IBankIdService
    {
        string StartOrder(string personalId);
        Task<BankIdStatusResult> GetStatusAsync(string orderRef);
    }
}