namespace NordiskaPortal.Api.Services.BankId;

public sealed class BankIdOptions
{
    public const string SectionName = "BankId";

    // false = mock (default). true = BankID's test server; requires the certificates below.
    public bool UseRealApi { get; set; }

    // Must end with "/" so relative paths like "auth" are appended, not replaced.
    public string BaseUrl { get; set; } = "https://appapi2.test.bankid.com/rp/v6.0/";

    // Windows: thumbprint of the client certificate in CurrentUser\My.
    public string? ClientCertificateThumbprint { get; set; }

    // Linux/Docker: path and password of the client .p12 file.
    public string? ClientCertificatePath { get; set; }
    public string? ClientCertificatePassword { get; set; }

    // BankID's CA (issuer of their server certificate), relative to the content root.
    public string? CaCertificatePath { get; set; }
}