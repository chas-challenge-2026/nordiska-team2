using System.Text.Json.Serialization;
using NordiskaPortal.Api.Models;

namespace NordiskaPortal.Api.DTOs
{
    // Amount is signed: positive = received (deposit), negative = spent (withdrawal).
    public record LedgerEntryDto(
        DateTime Date,
        string? Description,
        decimal Amount,
        [property: JsonConverter(typeof(JsonStringEnumConverter))] TransactionType Type,
        string? CounterpartAccountNumber = null);
}