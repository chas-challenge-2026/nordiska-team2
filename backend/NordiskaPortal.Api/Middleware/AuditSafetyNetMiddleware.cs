using Microsoft.EntityFrameworkCore;
using NordiskaPortal.Api.Data;
using NordiskaPortal.Api.Models;
using NordiskaPortal.Api.Services;

namespace NordiskaPortal.Api.Middleware
{
    // Safety net for the audit log. 
    // Does NOT replace explicit _audit.Record(...) calls, 
    // those stay the real audit trail precise with the action they describe.
    //
    // What this catches: a request that successfully changed something
    // (POST/PUT/PATCH/DELETE answered with 2xx) 
    // but left no saved audit entry. 
    // 
    // That happens in two ways:
    // 1. A new endpoint whose developer never called Record().
    // 2. Record() was called AFTER SaveChangesAsync(), so the entry was staged but never saved.
    //
    // When it fires, it (a) logs a warning naming the endpoint, so the
    // developer finds out, and (b) writes a fallback "unaudited_request"
    // row, so the event isn't missing from the database entirely.
    public class AuditSafetyNetMiddleware
    {
        private static readonly HashSet<string> StateChangingMethods =
            new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" };

        private readonly RequestDelegate _next;
        private readonly ILogger<AuditSafetyNetMiddleware> _logger;

        public AuditSafetyNetMiddleware(RequestDelegate next, ILogger<AuditSafetyNetMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IServiceScopeFactory scopeFactory)
        {
            await _next(context);

            // Everything below runs after the endpoint has finished.
            try
            {
                await CheckAsync(context, scopeFactory);
            }
            catch (Exception ex)
            {
                // The safety net must never break a request that already succeeded. Log and move on.
                _logger.LogError(ex, "Audit safety net failed for {Method} {Path}",
                    context.Request.Method, context.Request.Path);
            }
        }

        private async Task CheckAsync(HttpContext context, IServiceScopeFactory scopeFactory)
        {
            if (!StateChangingMethods.Contains(context.Request.Method))
                return;

            var status = context.Response.StatusCode;
            if (status < 200 || status >= 300)
                return;

            var endpoint = context.GetEndpoint();
            if (endpoint?.Metadata.GetMetadata<SkipAuditCheckAttribute>() != null)
                return;

            // Same scoped instances the endpoint used during this request
            var audit = context.RequestServices.GetRequiredService<IAuditService>();
            var db = context.RequestServices.GetRequiredService<BankContext>();

            var stagedButUnsaved = db.ChangeTracker.Entries<AuditEntry>()
                .Any(e => e.State == EntityState.Added);

            if (audit.RecordCount > 0 && !stagedButUnsaved)
                return; // Properly audited, the normal case.

            // Route template ("api/transactions/transfer"), not the raw path: stable across requests and contains no ids.
            var route = (endpoint as RouteEndpoint)?.RoutePattern.RawText ?? context.Request.Path.Value ?? "?";
            var description = $"{context.Request.Method} {route} {status}";

            if (stagedButUnsaved)
            {
                _logger.LogError(
                    "Audit entry was staged but never saved for {Request}. " +
                    "_audit.Record() must be called BEFORE SaveChangesAsync().",
                    description);
            }
            else
            {
                _logger.LogWarning(
                    "Unaudited state-changing request: {Request}. Add _audit.Record(...) to this " +
                    "feature, or mark the endpoint [SkipAuditCheck(\"reason\")] if it changes nothing.",
                    description);
            }

            await WriteFallbackAsync(context, scopeFactory, description);
        }

        private static async Task WriteFallbackAsync(
            HttpContext context, IServiceScopeFactory scopeFactory, string description)
        {
            // A fresh scope, so this save can't accidentally commit anything else the endpoint left half-done in its own DbContext.
            using var scope = scopeFactory.CreateScope();
            var audit = scope.ServiceProvider.GetRequiredService<IAuditService>();
            var db = scope.ServiceProvider.GetRequiredService<BankContext>();

            int? customerId = int.TryParse(context.User.FindFirst("sub")?.Value, out var id) ? id : null;

            // RefId MaxLength(50).
            var refId = description.Length > 50 ? description[..50] : description;

            audit.Record(AuditActions.UnauditedRequest, customerId, refId);
            await db.SaveChangesAsync();
        }
    }
}