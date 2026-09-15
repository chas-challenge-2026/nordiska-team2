// Contract for login/refresh/logout.
// Returns null/void on expected.
// Fails (wrong password, expired token), instead of throwing.
namespace NordiskaPortal.Api.Services
{
    public record AuthResult(string AccessToken, string RefreshToken);

    public interface IAuthService
    {
        Task<AuthResult?> LoginAsync(string email, string password);
        Task<AuthResult?> RefreshAsync(string refreshToken);
        Task LogoutAsync(string refreshToken);
    }
}