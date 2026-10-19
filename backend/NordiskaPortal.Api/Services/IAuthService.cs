namespace NordiskaPortal.Api.Services
{
    public record AuthResult(string AccessToken, string RefreshToken);

    public interface IAuthService
    {
        Task<AuthResult?> LoginAsync(string email, string password);
        Task<AuthResult?> RefreshAsync(string refreshToken);
        Task LogoutAsync(string refreshToken);

        // Exposed for public id-confirmation flow (BankID mock/real)
        Task<AuthResult> IssueTokensForCustomerAsync(int customerId, string email);
    }
}