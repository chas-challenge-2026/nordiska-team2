// For Dashboard: id, number, type, rate, balance.
namespace NordiskaPortal.Api.DTOs
{
    public record AccountDto(int Id, string AccountNumber, string AccountType, decimal InterestRate, decimal Balance);
}