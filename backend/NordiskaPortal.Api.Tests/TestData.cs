using NordiskaPortal.Api.Models;

namespace NordiskaPortal.Api.Tests
{
    // Creates a fresh customer with two accounts for one test.
    // Tests that assert exact balances MUST use this instead of the seeded
    // Anna/Erik accounts: all test classes share one database, so any other
    // test that touches the seeded accounts changes their balances.
    internal static class TestData
    {
        public static async Task<(int CustomerId, int AccountA, int AccountB)> CreateCustomerWithAccountsAsync(
            PostgresFixture fixture, decimal openingBalanceOnA)
        {
            await using var db = fixture.CreateContext();
            var unique = Guid.NewGuid().ToString("N")[..8];

            var customer = new Customer
            {
                Name = $"Test {unique}",
                PersonalId = $"19{Random.Shared.Next(100000, 999999)}-{Random.Shared.Next(1000, 9999)}",
                Address = "Testgatan 1",
                Email = $"t{unique}@test.local",
                PasswordHash = "not-used-in-these-tests",
                CreatedAt = DateTime.UtcNow
            };
            db.Customers.Add(customer);
            await db.SaveChangesAsync();

            var a = NewAccount(customer.Id, $"TA-{unique}");
            var b = NewAccount(customer.Id, $"TB-{unique}");
            db.SavingsAccounts.AddRange(a, b);
            await db.SaveChangesAsync();

            if (openingBalanceOnA > 0)
            {
                var now = DateTime.UtcNow;
                db.Transactions.Add(new Transaction
                {
                    AccountId = a.Id,
                    Type = TransactionType.Deposit,
                    Description = "Startsaldo",
                    Amount = openingBalanceOnA,
                    TransactionDate = now,
                    PostingDate = Transaction.CalculatePostingDate(now),
                    Status = TransactionStatus.Posted
                });
                await db.SaveChangesAsync();
            }

            return (customer.Id, a.Id, b.Id);
        }

        private static SavingsAccount NewAccount(int customerId, string accountNumber) => new()
        {
            CustomerId = customerId,
            AccountNumber = accountNumber,
            InterestRate = 0.0300m,
            AccountType = "Savings",
            CreatedAt = DateTime.UtcNow
        };
    }
}