namespace NordiskaPortal.Api.DTOs
{
    // Never include PasswordHash here or in any DTO that leaves the service layer
    // Customer-facing shape, not the database row.
    public record CustomerDto(int Id, string Name, string Email, string PersonalId, string Address);
}