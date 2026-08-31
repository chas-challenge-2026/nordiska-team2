namespace NordiskaPortal.Api.DTOs;

public record DepositRequest(Guid AccountId, decimal Amount);

public record WithdrawRequest(Guid AccountId, decimal Amount);

public record TransactionDto(Guid Id, decimal Amount, string Type, DateTime CreatedAt);