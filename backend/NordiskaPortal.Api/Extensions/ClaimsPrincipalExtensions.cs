using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

// Extracts the authenticated customer's ID from JWT claims. 
// Every [Authorize]-protected endpoint uses this instead of trusting an ID
// from the URL or request body.

// Fixes: "anyone can access anyone's account"

namespace NordiskaPortal.Api.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static int GetCustomerId(this ClaimsPrincipal user)
        {
            var subClaim = user.FindFirst(JwtRegisteredClaimNames.Sub)
                ?? throw new InvalidOperationException("No sub claim found on the authenticated user.");

            if (!int.TryParse(subClaim.Value, out var customerId))
                throw new InvalidOperationException("Sub claim is not a valid customer id.");

            return customerId;
        }
    }
}