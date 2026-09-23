using NordiskaPortal.Api.DTOs;

namespace NordiskaPortal.Api.Services
{
    public interface ICustomerService
    {
        Task<CustomerDto?> GetCustomerAsync(int customerId);
    }
}