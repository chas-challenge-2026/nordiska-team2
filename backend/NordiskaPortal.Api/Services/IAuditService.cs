namespace NordiskaPortal.Api.Services
{
    public interface IAuditService
    {
        // Stages an audit row on the shared DbContext WITHOUT saving it.
        // The caller's own SaveChangesAsync() then commits the action and its audit row together, in one database transaction.
        void Record(string action, int? customerId, string? refId = null, string? actor = null);

        Task<List<DTOs.AuditEntryDto>> GetForCustomerAsync(int customerId, int limit = 50);

        // How many entries this request has staged. Read by AuditSafetyNetMiddleware.
        int RecordCount { get; }
    }
}