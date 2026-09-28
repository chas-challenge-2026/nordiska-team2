namespace NordiskaPortal.Api.DTOs
{
    // What the customer sees in their activity log. Deliberately omits
    // Actor, CorrelationId and Signature -- internal fields with no
    // meaning to a customer.
    public record AuditEntryDto(string Action, string? RefId, DateTime Timestamp, string? IpAddress);
}