using NordiskaPortal.Api.DTOs;
using NordiskaPortal.Api.Models;

namespace NordiskaPortal.Api.Services
{
    public record TransactionResult(bool Success, string? Error, LedgerEntryDto? Entry);

    // No Update or Delete here, immutable and transactions are append-only
    // Corrections never edits an existing row and instead are new offsetting transactions.
    public interface ITransactionService
    {
        Task<decimal> GetBalanceAsync(int accountId);
        Task<TransactionResult> DepositAsync(int accountId, decimal amount, string? description = null);
        Task<TransactionResult> WithdrawAsync(int accountId, decimal amount, string? description = null);
        Task<List<LedgerEntryDto>> GetHistoryAsync(int accountId);
        Task<TransactionResult> TransferAsync(int fromAccountId, int toAccountId, decimal amount, string? description = null);
    }
}