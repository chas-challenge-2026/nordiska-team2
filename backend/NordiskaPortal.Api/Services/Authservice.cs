using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NordiskaPortal.Api.Data;
using NordiskaPortal.Api.Models;

// BCrypt password verification + JWT issuance, with rotating hashed refresh tokens.
// Every refresh revokes the old token and issues a new pair, so a replayed stolen token is detectable.
namespace NordiskaPortal.Api.Services
{
    public class AuthService : IAuthService
    {
        private readonly BankContext _db;
        private readonly IConfiguration _config;

        public AuthService(BankContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        public async Task<AuthResult?> LoginAsync(string email, string password)
        {
            var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Email == email);
            if (customer == null)
                return null;

            if (!BCrypt.Net.BCrypt.Verify(password, customer.PasswordHash))
                return null;

            return await IssueTokensAsync(customer.Id, customer.Email);
        }

        public async Task<AuthResult?> RefreshAsync(string refreshToken)
        {
            var hash = HashToken(refreshToken);
            var stored = await _db.RefreshTokens
                .Include(rt => rt.Customer)
                .FirstOrDefaultAsync(rt => rt.TokenHash == hash);

            if (stored == null || !stored.IsActive || stored.Customer == null)
                return null;

            stored.RevokedAt = DateTime.UtcNow;

            return await IssueTokensAsync(stored.CustomerId, stored.Customer.Email);
        }

        public async Task LogoutAsync(string refreshToken)
        {
            var hash = HashToken(refreshToken);
            var stored = await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == hash);

            if (stored != null && stored.RevokedAt == null)
            {
                stored.RevokedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }

        private async Task<AuthResult> IssueTokensAsync(int customerId, string email)
        {
            var accessToken = GenerateAccessToken(customerId, email);
            var refreshTokenRaw = GenerateRefreshTokenValue();

            var refreshTokenDays = _config.GetValue<int>("Jwt:RefreshTokenExpiryDays", 7);
            _db.RefreshTokens.Add(new RefreshToken
            {
                CustomerId = customerId,
                TokenHash = HashToken(refreshTokenRaw),
                ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenDays),
            });

            await _db.SaveChangesAsync();

            return new AuthResult(accessToken, refreshTokenRaw);
        }

        private string GenerateAccessToken(int customerId, string email)
        {
            var key = _config["Jwt:Key"]
                ?? throw new InvalidOperationException(
                    "Jwt:Key is not configured. Run: dotnet user-secrets set \"Jwt:Key\" \"your-key\"");

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, customerId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            var expiryMinutes = _config.GetValue<int>("Jwt:AccessTokenExpiryMinutes", 15);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string GenerateRefreshTokenValue()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(bytes);
        }

        private static string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(bytes);
        }
    }
}