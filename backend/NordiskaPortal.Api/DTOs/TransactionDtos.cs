namespace NordiskaPortal.Api.DTOs
{
    public record DepositRequest(int AccountId, decimal Amount, string? Description = null);

    public record WithdrawRequest(int AccountId, decimal Amount, string? Description = null);
}