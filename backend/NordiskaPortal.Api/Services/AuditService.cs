using Microsoft.EntityFrameworkCore;
using NordiskaPortal.Api.Data;
using NordiskaPortal.Api.DTOs;
using NordiskaPortal.Api.Models;

namespace NordiskaPortal.Api.Services
{
    public class AuditService : IAuditService
    {
        private readonly BankContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public int RecordCount { get; private set; }

        // BankContext is Scoped, and so is this service. 
        // Within one HTTP request, DI hands TransactionService and AuditService the same BankContext instance,
        // that shared instance is the whole mechanism behind "audit write in the same transaction".
        public AuditService(BankContext db, IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _httpContextAccessor = httpContextAccessor;
        }

        public void Record(string action, int? customerId, string? refId = null, string? actor = null)
        {
            RecordCount++;

            // HttpContext is null outside a request (background jobs, tests). Every field below tolerates that.
            var http = _httpContextAccessor.HttpContext;

            _db.AuditEntries.Add(new AuditEntry
            {
                Actor = actor ?? customerId?.ToString() ?? "anonymous",
                CustomerId = customerId,
                Action = action,
                RefId = refId,
                Timestamp = DateTime.UtcNow,
                IpAddress = http?.Connection.RemoteIpAddress?.ToString(),
                CorrelationId = http?.TraceIdentifier,
            });

            // No SaveChangesAsync here, on purpose. 
            //
            // If this method saved, the audit row would commit on its own and could exist for an
            // action that then failed or the action could commit while this save failed.
            //
            // Staging it lets the caller's save decide for both.
        }

        public async Task<List<AuditEntryDto>> GetForCustomerAsync(int customerId, int limit = 50)
        {
            return await _db.AuditEntries
                .AsNoTracking()
                .Where(a => a.CustomerId == customerId)
                .Where(a => a.Action != AuditActions.UnauditedRequest) 
                .OrderByDescending(a => a.Timestamp)
                .Take(limit)
                .Select(a => new AuditEntryDto(a.Action, a.RefId, a.Timestamp, a.IpAddress))
                .ToListAsync();
        }

        
    }
}