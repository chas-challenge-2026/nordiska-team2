namespace NordiskaPortal.Api.Errors
{
    // Every business error the API can return, in one place.
    // Adding one: add it here, plus a key with the same code in every
    // SharedResource.*.resx (the tests fail until you do).
    public static class ApiErrors
    {
        public static readonly ApiError InvalidAmount = new("invalid_amount");
        public static readonly ApiError AccountNotFound = new("account_not_found");
        public static readonly ApiError InsufficientFunds = new("insufficient_funds");
        public static readonly ApiError ConcurrencyConflict = new("concurrency_conflict");
        public static readonly ApiError SameAccountTransfer = new("same_account_transfer");
        public static readonly ApiError AccountNotEmpty = new("account_not_empty");

        public static ApiError TooManyAccounts(int max) =>
            new("too_many_accounts", new Dictionary<string, object> { ["max"] = max });
    }
}