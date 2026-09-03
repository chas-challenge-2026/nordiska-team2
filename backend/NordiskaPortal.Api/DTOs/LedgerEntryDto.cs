namespace NordiskaPortal.Api.DTOs
{
    // Amount is signed: positive = received (deposit), negative = spent (withdrawal).
    public record LedgerEntryDto(DateTime Date, string Description, decimal Amount);
}