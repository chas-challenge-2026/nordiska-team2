using NordiskaPortal.Api.Services.BankId;

public class BankIdQrCodeTests
{
    private const string Token  = "67df3917-fa0d-44e5-b327-edcc928297f8";
    private const string Secret = "d28db9a7-4cde-429e-a983-359be676944c";

    [Theory]
    [InlineData(0, "dc69358e712458a66a7525beef148ae8526b1c71610eff2c16cdffb4cdac9bf8")]
    [InlineData(1, "949d559bf23403952a94d103e67743126381eda00f0b3cbddbf7c96b1adcbce2")]
    [InlineData(2, "a9e5ec59cb4eee4ef4117150abc58fad7a85439a6a96ccbecc3668b41795b3f3")]
    public void Generate_MatchesBankIdsPublishedExamples(long seconds, string expectedAuthCode)
    {
        var qr = BankIdQrCode.Generate(Token, Secret, seconds);

        Assert.Equal($"bankid.{Token}.{seconds}.{expectedAuthCode}", qr);
    }

    [Fact]
    public void Generate_CountsWholeSecondsSinceOrderStart()
    {
        var start = DateTimeOffset.UtcNow;

        var qr = BankIdQrCode.Generate(Token, Secret, start, start.AddMilliseconds(1999));

        Assert.Equal(BankIdQrCode.Generate(Token, Secret, 1), qr);
    }
}