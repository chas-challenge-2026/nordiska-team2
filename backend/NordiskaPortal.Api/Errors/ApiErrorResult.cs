using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace NordiskaPortal.Api.Errors
{
    // The JSON body of every business error:
    // { "code": "too_many_accounts", "error": "You can have at most 10 open accounts.", "params": { "max": 10 } }
    // "error" keeps its old name, so clients reading response.data.error keep working.
    public sealed record ApiErrorBody(
        string Code,
        string Error,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        IReadOnlyDictionary<string, object>? Params);

    // Turns an ApiError into a response, with the text in the request's language
    // (set by UseRequestLocalization from Accept-Language). Gets the localizer
    // from the request itself, so controllers need no new constructor parameters.
    public sealed class ApiErrorResult : ActionResult
    {
        private readonly ApiError _error;
        private readonly int _statusCode;

        public ApiErrorResult(ApiError error, int statusCode = StatusCodes.Status400BadRequest)
        {
            _error = error;
            _statusCode = statusCode;
        }

        public override Task ExecuteResultAsync(ActionContext context)
        {
            var services = context.HttpContext.RequestServices;
            var text = services.GetRequiredService<IStringLocalizer<SharedResource>>()[_error.Code];

            // Should never happen (the catalog test checks it), but if it does,
            // say so in the log instead of failing silently.
            if (text.ResourceNotFound)
                services.GetRequiredService<ILogger<ApiErrorResult>>()
                    .LogWarning("No text for error code {Code} in SharedResource.resx", _error.Code);

            var body = new ApiErrorBody(_error.Code, Fill(text.Value, _error.Params), _error.Params);
            return new ObjectResult(body) { StatusCode = _statusCode }.ExecuteResultAsync(context);
        }

        // Fills named placeholders: "at most {max} accounts" + { max: 10 }.
        private static string Fill(string template, IReadOnlyDictionary<string, object>? values)
        {
            if (values is null) return template;
            foreach (var (name, value) in values)
                template = template.Replace("{" + name + "}", Convert.ToString(value, CultureInfo.CurrentCulture));
            return template;
        }
    }
}