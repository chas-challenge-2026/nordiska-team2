using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace NordiskaPortal.Api.Middleware
{
    /*
        Implements the built-in IExceptionHandler interface (.NET 8+), registered
        via AddExceptionHandler<T>() + UseExceptionHandler() in Program.cs. 
        
        This is the modern replacement for a hand-rolled try/catch middleware class. 
        Matches kravställningen's literal wording ("IExceptionHandler middleware").
    */
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            /*
                TraceIdentifier is ASP.NET Core's built-in per-request ID
                and used here as the correlation ID. 
                
                TODO: Swap for a custom X-Correlation-Id header value later 
                if one that flows across services is needed.
                (e.g. into the native PDF module or a future BankID call).
            */
            var correlationId = httpContext.TraceIdentifier;

            _logger.LogError(
                exception,
                "Unhandled exception. CorrelationId: {CorrelationId}",
                correlationId);

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            httpContext.Response.Headers["X-Correlation-Id"] = correlationId;

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Ett oväntat fel inträffades.",
                Detail = "Kontakta support med detta CorrelationId om felet kvarstår.",
            };
            problemDetails.Extensions["correlationId"] = correlationId;

            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            // true = "I handled this, stop looking for another handler"
            return true;
        }
    }
}