using Microsoft.EntityFrameworkCore;
using NordiskaPortal.Api.Service;
using NordiskaPortal.Api.Data;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();

// Swagger
builder.Services.AddSwaggerGen();

// Health checks
builder.Services.AddHealthChecks();

// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // TODO Update Rate Limiter to be customer based.
    // "SensitiveEndpoints" from PingController.cs is a stand in and is ping based for testing purposes.
    options.AddPolicy("SensitiveEndpoints", httpContext =>
    {
        var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
    });
});

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options =>
    {
        options.RouteTemplate = "/openapi/{documentName}.json";
    });
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

// Rate Limiter
app.UseRateLimiter();

app.MapControllers();

// Health check endpoint
app.MapHealthChecks("/health");

app.Run();