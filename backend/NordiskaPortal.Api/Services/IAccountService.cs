using NordiskaPortal.Api.DTOs;

// Contract for fetching a customer's accounts with computed balances,
// and for checking account ownership before allowing an operation.
namespace NordiskaPortal.Api.Services
{
    public record AccountResult(bool Success, string? Error, AccountDto? Account);
    public interface IAccountService
    {
        Task<List<AccountDto>> GetAccountsForCustomerAsync(int customerId);
        Task<bool> CustomerOwnsAccountAsync(int customerId, int accountId);
        Task<FinancialSummaryDto> GetFinancialSummaryAsync(int customerId, DateTime from, DateTime to);
        Task<AccountResult> OpenAccountAsync(int customerId);
        Task<AccountResult> CloseAccountAsync(int customerId, int accountId);
    }
}