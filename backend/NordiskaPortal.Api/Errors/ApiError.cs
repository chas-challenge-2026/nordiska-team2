namespace NordiskaPortal.Api.Errors
{
    // A business error as data: a stable code (a contract with the frontend's
    // errors.* keys, never rename one) plus optional values for the text,
    // e.g. { max: 10 }. No text here: that lives in SharedResource.*.resx.
    public sealed record ApiError(string Code, IReadOnlyDictionary<string, object>? Params = null);
}