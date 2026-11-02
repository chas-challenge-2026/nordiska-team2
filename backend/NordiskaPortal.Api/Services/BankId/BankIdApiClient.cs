using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;

namespace NordiskaPortal.Api.Services.BankId;

public sealed record BankIdAuthResponse(string OrderRef, string AutoStartToken, string QrStartToken, string QrStartSecret);
public sealed record BankIdCollectResponse(string OrderRef, string Status, string? HintCode, BankIdCompletionData? CompletionData);
public sealed record BankIdCompletionData(BankIdUser User);
public sealed record BankIdUser(string PersonalNumber, string Name, string GivenName, string Surname);
internal sealed record BankIdErrorBody(string? ErrorCode, string? Details);

public sealed class BankIdApiException(HttpStatusCode statusCode, string? errorCode, string? details)
    : Exception($"BankID returned {(int)statusCode} {errorCode}: {details}")
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string? ErrorCode { get; } = errorCode;
}

public interface IBankIdApiClient
{
    Task<BankIdAuthResponse> AuthAsync(string endUserIp, CancellationToken ct = default);
    Task<BankIdCollectResponse> CollectAsync(string orderRef, CancellationToken ct = default);
    Task CancelAsync(string orderRef, CancellationToken ct = default);
}

public sealed class BankIdApiClient(HttpClient http) : IBankIdApiClient
{
    public Task<BankIdAuthResponse> AuthAsync(string endUserIp, CancellationToken ct = default) =>
        PostAsync<BankIdAuthResponse>("auth", new { endUserIp }, ct);

    public Task<BankIdCollectResponse> CollectAsync(string orderRef, CancellationToken ct = default) =>
        PostAsync<BankIdCollectResponse>("collect", new { orderRef }, ct);

    public Task CancelAsync(string orderRef, CancellationToken ct = default) =>
        PostAsync<JsonElement>("cancel", new { orderRef }, ct);

    // No leading "/" on paths, so they're appended to BaseAddress (".../rp/v6.0/auth").
    private async Task<T> PostAsync<T>(string path, object body, CancellationToken ct)
    {
        var content = JsonContent.Create(body);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        using var response = await http.PostAsync(path, content, ct);
        if (!response.IsSuccessStatusCode)
        {
            BankIdErrorBody? error = null;
            try { error = await response.Content.ReadFromJsonAsync<BankIdErrorBody>(cancellationToken: ct); }
            catch (JsonException) { /* body wasn't BankID's error format */ }
            throw new BankIdApiException(response.StatusCode, error?.ErrorCode, error?.Details);
        }
        return (await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct))!;
    }
}