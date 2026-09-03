using NordiskaPortal.Api.DTOs;

// Contract for fetching a customer's accounts with computed balances.
namespace NordiskaPortal.Api.Services
{
    public interface IAccountService
    {
        Task<List<AccountDto>> GetAccountsForCustomerAsync(int customerId);
    }
}