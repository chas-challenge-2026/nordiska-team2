using System.Security.Cryptography.X509Certificates;

namespace NordiskaPortal.Api.Services.BankId;

public sealed class BankIdCertificates
{
    public required X509Certificate2 Client { get; init; }
    public required X509Certificate2 Root { get; init; }

    public static BankIdCertificates Load(BankIdOptions options, string contentRoot)
    {
        var client = LoadClient(options);
        if (!client.HasPrivateKey)
            throw new InvalidOperationException("The BankID client certificate has no private key.");

        if (string.IsNullOrWhiteSpace(options.CaCertificatePath))
            throw new InvalidOperationException("BankId:CaCertificatePath is not set.");

        var caPath = Path.Combine(contentRoot, options.CaCertificatePath);
        if (!File.Exists(caPath))
            throw new InvalidOperationException($"BankID CA certificate not found at '{caPath}'.");

        var root = X509Certificate2.CreateFromPem(File.ReadAllText(caPath));
        return new BankIdCertificates { Client = client, Root = root };
    }

    private static X509Certificate2 LoadClient(BankIdOptions options)
    {
        // Windows: from the certificate store. Our curl test showed Windows can't use the
        // private key straight from the .p12 file, but works once it's in the store.
        if (!string.IsNullOrWhiteSpace(options.ClientCertificateThumbprint))
        {
            var thumbprint = options.ClientCertificateThumbprint.Replace(" ", "");
            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            // validOnly: false, since Windows doesn't trust the test certificate's issuer.
            var matches = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);
            return matches.Count > 0
                ? matches[0]
                : throw new InvalidOperationException(
                    $"No certificate with thumbprint {thumbprint} in CurrentUser\\My. Import the BankID test .p12 first.");
        }

        // Linux/Docker: from the .p12 file.
        if (!string.IsNullOrWhiteSpace(options.ClientCertificatePath))
        {
            if (!File.Exists(options.ClientCertificatePath))
                throw new InvalidOperationException($"BankID client certificate not found at '{options.ClientCertificatePath}'.");
            return new X509Certificate2(options.ClientCertificatePath, options.ClientCertificatePassword);
        }

        throw new InvalidOperationException(
            "BankId:UseRealApi is true, but neither BankId:ClientCertificateThumbprint nor BankId:ClientCertificatePath is set.");
    }
}