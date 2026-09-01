using NordiskaPortal.Api.DTOs;
using NordiskaPortal.Api.Models;

namespace NordiskaPortal.Api.Services
{
    public record TransactionResult(bool Success, string? Error, Transaction? Transaction);

    // No Update or Delete here, immutable and transactions are append-only
    // Corrections never edits an existing row and instead are new offsetting transactions.
    public interface ITransactionService
    {
        Task<decimal> GetBalanceAsync(int accountId);
        Task<TransactionResult> DepositAsync(int accountId, decimal amount);
        Task<TransactionResult> WithdrawAsync(int accountId, decimal amount);
        Task<List<LedgerEntryDto>> GetHistoryAsync(int accountId);
    }
}