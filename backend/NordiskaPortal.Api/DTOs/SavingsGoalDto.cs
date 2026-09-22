namespace NordiskaPortal.Api.DTOs
{
    public record CreateSavingsGoalRequest(string Name, decimal TargetAmount, DateTime? Deadline, int? AccountId);
    
    public record SavingsGoalDto(
        int Id,
        string Name,
        decimal TargetAmount,
        DateTime? Deadline,
        int? AccountId,
        decimal? CurrentAmount,
        decimal? ProgressPercent);
}