using NordiskaPortal.Api.DTOs;

// Contract for fetching a customer's accounts with computed balances,
// and for checking account ownership before allowing an operation.
namespace NordiskaPortal.Api.Services
{
    public interface IAccountService
    {
        Task<List<AccountDto>> GetAccountsForCustomerAsync(int customerId);
        Task<bool> CustomerOwnsAccountAsync(int customerId, int accountId);
    }
}