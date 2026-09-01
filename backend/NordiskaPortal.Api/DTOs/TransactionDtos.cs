namespace NordiskaPortal.Api.DTOs
{
    public record DepositRequest(int AccountId, decimal Amount);

    public record WithdrawRequest(int AccountId, decimal Amount);
}