namespace NordiskaPortal.Api.DTOs
{
    public record DepositRequest(int AccountId, decimal Amount, string? Description = null);

    public record WithdrawRequest(int AccountId, decimal Amount, string? Description = null);

    public record TransferRequest(int FromAccountId, int ToAccountId, decimal Amount, string? Description = null);
}