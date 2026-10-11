using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using NordiskaPortal.Assistant.Contracts;
using NordiskaPortal.Assistant.Options;
using NordiskaPortal.Assistant.Services;

// NordiskaPortal.Assistant -- a deliberately independent service.
// Its ONLY link to the banking system: it accepts the same JWTs (same
// Issuer/Audience/Key). No project reference, no database, no customer data.
// To scrap it: delete this project and its Docker service.

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// No fallback key, on purpose: a missing key must stop the app, never make it
// accept tokens signed with a key everyone can read in the repo.
var jwtKey = config["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "Jwt:Key is not configured. It must match NordiskaPortal.Api's key on this machine.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Same lesson as the banking API: without this, "sub" is silently
        // renamed to ClaimTypes.NameIdentifier and lookups for "sub" fail.
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = config["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = config["Jwt:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero        // same as the banking API
        };
    });
builder.Services.AddAuthorization();

// The frontend calls this service directly, so it needs its own CORS policy.
// No AllowCredentials: the token travels in the Authorization header and
// this service uses no cookies.
var origins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(o => o.AddPolicy("Frontend", p => p
    .WithOrigins(origins)
    .WithMethods("GET", "POST")
    .WithHeaders("Authorization", "Content-Type")));

// Per customer, not per IP: the caller is authenticated, so "sub" identifies
// them even when many share one IP (office, mobile network).
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("PerCustomer", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.User.FindFirst("sub")?.Value ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

builder.Services.AddProblemDetails();

builder.Services.Configure<AssistantOptions>(config.GetSection(AssistantOptions.SectionName));
builder.Services.AddSingleton<OllamaGate>();
builder.Services.AddHttpClient<AssistantService>((sp, http) =>
{
    var options = sp.GetRequiredService<IOptions<AssistantOptions>>().Value;
    // Trailing slash matters: without it, "api/chat" replaces the last path
    // segment instead of being appended.
    http.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
    http.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
});
builder.Services.Configure<RouteHandlerOptions>(o => o.ThrowOnBadRequest = false);

var app = builder.Build();

app.UseExceptionHandler();     // ProblemDetails instead of stack traces
app.UseCors("Frontend");
app.UseAuthentication();       // BEFORE the rate limiter, so "sub" exists
app.UseAuthorization();        // when the limiter picks a partition
app.UseRateLimiter();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

var assistant = app.MapGroup("/api/assistant")
    .RequireAuthorization()
    .RequireRateLimiting("PerCustomer");

// POST /api/assistant/chat   { "question": "..." } to { "answer": "..." }
assistant.MapPost("/chat", async (
    ChatRequest request,
    AssistantService service,
    IOptions<AssistantOptions> options,
    CancellationToken ct) =>
{
    var question = request.Question?.Trim();
 
    if (string.IsNullOrEmpty(question))
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["question"] = new[] { "Frågan får inte vara tom." }
        });
 
    if (question.Length > options.Value.MaxQuestionLength)
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["question"] = new[] { $"Frågan får vara högst {options.Value.MaxQuestionLength} tecken." }
        });
 
    var result = await service.AskAsync(question, ct);
 
    return result.Status switch
    {
        AssistantStatus.Ok => Results.Ok(new ChatResponse(result.Answer!)),
        AssistantStatus.Busy => Results.Problem(
            detail: "Assistenten är upptagen just nu. Försök igen om en stund.",
            statusCode: StatusCodes.Status503ServiceUnavailable),
        _ => Results.Problem(
            detail: "Assistenten är inte tillgänglig just nu.",
            statusCode: StatusCodes.Status503ServiceUnavailable)
    };
});

app.Run();

public partial class Program { }