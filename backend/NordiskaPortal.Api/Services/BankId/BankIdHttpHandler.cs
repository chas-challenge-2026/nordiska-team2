using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace NordiskaPortal.Api.Services.BankId;

public static class BankIdHttpHandler
{
    public static SocketsHttpHandler Create(BankIdCertificates certs, ILogger logger) => new()
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        SslOptions = new SslClientAuthenticationOptions
        {
            ClientCertificates = new X509CertificateCollection { certs.Client },
            RemoteCertificateValidationCallback = (_, serverCert, presentedChain, errors) =>
                IsTrustedBankIdServer(serverCert, presentedChain, errors, certs.Root, logger),
        },
    };

    // Trusts ONLY BankID's CA. Logs exactly why when it rejects a certificate.
    private static bool IsTrustedBankIdServer(
        X509Certificate? serverCert, X509Chain? presentedChain, SslPolicyErrors errors,
        X509Certificate2 root, ILogger logger)
    {
        if (serverCert is null)
        {
            logger.LogWarning("BankID server sent no certificate.");
            return false;
        }

        using var leaf = new X509Certificate2(serverCert);

        if ((errors & (SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateNotAvailable)) != 0)
        {
            logger.LogWarning("BankID server certificate rejected (host name). Subject={Subject} SslErrors={Errors}",
                leaf.Subject, errors);
            return false;
        }

        using var chain = new X509Chain();
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        chain.ChainPolicy.CustomTrustStore.Add(root);
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck; // test environment; Online in production

        if (presentedChain is not null)
            foreach (var element in presentedChain.ChainElements)
                chain.ChainPolicy.ExtraStore.Add(element.Certificate);

        var trusted = chain.Build(leaf);
        if (!trusted)
        {
            logger.LogWarning(
                "BankID server certificate rejected (chain). Subject={Subject} Issuer={Issuer} " +
                "SslErrors={Errors} ChainStatus={ChainStatus} PresentedChain={Presented} OurRoot={Root}",
                leaf.Subject,
                leaf.Issuer,
                errors,
                string.Join("; ", chain.ChainStatus.Select(s => $"{s.Status}: {s.StatusInformation.Trim()}")),
                presentedChain is null
                    ? "(none)"
                    : string.Join(" <- ", presentedChain.ChainElements.Select(e => e.Certificate.Subject)),
                root.Subject);
        }
        return trusted;
    }
}