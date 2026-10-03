namespace NordiskaPortal.Api.Middleware
{
    // Marks an endpoint that changes nothing worth auditing, so the
    // AuditSafetyNetMiddleware doesn't flag it.
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class SkipAuditCheckAttribute : Attribute
    {
        public string Reason { get; }

        public SkipAuditCheckAttribute(string reason)
        {
            Reason = reason;
        }
    }
}