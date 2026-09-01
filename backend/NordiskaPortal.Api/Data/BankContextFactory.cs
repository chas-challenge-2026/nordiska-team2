using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace NordiskaPortal.Api.Data
{
    /*
        Used only by `dotnet ef` commands (migrations add, database update).

        Bypasses Program.cs at design time, so EF tooling doesn't need
        to build the whole app (DI, rate limiter, controllers, etc.) just to
        find a connection string.
    */

    public class BankContextFactory : IDesignTimeDbContextFactory<BankContext>
    {
        public BankContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddUserSecrets<BankContext>(optional: true)
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");

            var optionsBuilder = new DbContextOptionsBuilder<BankContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new BankContext(optionsBuilder.Options);
        }
    }
}