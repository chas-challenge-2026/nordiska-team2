using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace NordiskaPortal.Api.Services.BankId;

public static class BankIdQrCode
{
    // Elapsed time is measured from when the order was created.
    public static string Generate(string qrStartToken, string qrStartSecret,
                                  DateTimeOffset orderCreatedAt, DateTimeOffset now)
    {
        var seconds = Math.Max(0, (long)Math.Floor((now - orderCreatedAt).TotalSeconds));
        return Generate(qrStartToken, qrStartSecret, seconds);
    }

    // The same input always gives the same output, so it's easy to test.
    public static string Generate(string qrStartToken, string qrStartSecret, long seconds)
    {
        var time = seconds.ToString(CultureInfo.InvariantCulture);
        var hash = HMACSHA256.HashData(
            Encoding.ASCII.GetBytes(qrStartSecret),
            Encoding.ASCII.GetBytes(time));
        var qrAuthCode = Convert.ToHexString(hash).ToLowerInvariant();
        return $"bankid.{qrStartToken}.{time}.{qrAuthCode}";
    }
}