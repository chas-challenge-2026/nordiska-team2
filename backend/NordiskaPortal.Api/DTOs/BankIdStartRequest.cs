namespace NordiskaPortal.Api.DTOs
{
    // Real BankID doesn't require a personal number up front for every flow.
    // Simplest way to check for correct user for a mock for now.
    public record BankIdStartRequest(string PersonalId);
}