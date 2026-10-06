using Npgsql;

namespace NordiskaPortal.Api.Data
{
    // Recognizes specific Postgres errors, whether thrown directly or wrapped in EF Core's DbUpdateException.
    public static class PostgresErrors
    {
        // SQLSTATE 40001: Serializable isolation detected a conflicting concurrent transaction. The only error that means "try again".
        public static bool IsSerializationFailure(Exception ex) => Has(ex, PostgresErrorCodes.SerializationFailure);

        // SQLSTATE 23505: a unique index rejected a duplicate value.
        public static bool IsUniqueViolation(Exception ex) => Has(ex, PostgresErrorCodes.UniqueViolation);

        private static bool Has(Exception ex, string sqlState) =>
            ex is PostgresException { SqlState: var s1 } && s1 == sqlState
            || ex.InnerException is PostgresException { SqlState: var s2 } && s2 == sqlState;
    }
}