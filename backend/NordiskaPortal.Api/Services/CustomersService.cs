using Microsoft.EntityFrameworkCore;
using NordiskaPortal.Api.Data;
using NordiskaPortal.Api.DTOs;

namespace NordiskaPortal.Api.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly BankContext _db;

        public CustomerService(BankContext db)
        {
            _db = db;
        }

        public async Task<CustomerDto?> GetCustomerAsync(int customerId)
        {
            var customer = await _db.Customers.FindAsync(customerId);
            if (customer == null)
                return null;

            return new CustomerDto(customer.Id, customer.Name, customer.Email, customer.PersonalId, customer.Address);
        }
    }
}