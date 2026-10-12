using Microsoft.AspNetCore.Http;
using NordiskaPortal.Api.Data;
using NordiskaPortal.Api.Services;

namespace NordiskaPortal.Api.Tests
{
    // Builds a real AuditService for tests.
    //
    // Takes the DbContext as a parameter on purpose: the audit service
    // MUST share the context of the service under test, exactly like in
    // production. Give it a separate context and staged audit rows would
    // never be saved by the service's SaveChangesAsync -- the tests would
    // pass while silently auditing nothing.
    //
    // A fresh HttpContextAccessor has no HttpContext, so IpAddress and
    // CorrelationId come out null -- AuditService handles that already,
    // since background jobs have no request either.
    internal static class AuditServiceTests
    {
        public static IAuditService For(BankContext db) => new AuditService(db, new HttpContextAccessor());
    }
}