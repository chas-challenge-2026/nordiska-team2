using Microsoft.Extensions.Time.Testing;
using NordiskaPortal.Api.Services;

public class MockBankIdServiceTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 27, 12, 0, 0, TimeSpan.Zero));
    private readonly MockBankIdService _service;

    public MockBankIdServiceTests()
    {
        var store = new NordiskaPortal.Api.Services.BankId.BankIdOrderStore(_time);
        // These tests never reach the database: every path they exercise returns before the customer lookup.
        _service = new MockBankIdService(store, _time, db: null!, authService: null!);
    }

    [Fact]
    public async Task Start_ReturnsOrderRefAndAutoStartToken()
    {
        var start = await _service.StartAsync("127.0.0.1");

        Assert.False(string.IsNullOrWhiteSpace(start.OrderRef));
        Assert.False(string.IsNullOrWhiteSpace(start.AutoStartToken));
    }

    [Fact]
    public async Task Status_BeforeApproval_IsPendingWithQrData()
    {
        var start = await _service.StartAsync("127.0.0.1");

        var status = await _service.GetStatusAsync(start.OrderRef);

        Assert.Equal("pending", status.Status);
        Assert.Equal(BankIdHintCodes.OutstandingTransaction, status.HintCode);
        Assert.StartsWith("bankid.", status.QrData);
        Assert.Null(status.AccessToken);
    }

    [Fact]
    public async Task Status_QrData_ChangesEverySecond()
    {
        var start = await _service.StartAsync("127.0.0.1");
        var first = await _service.GetStatusAsync(start.OrderRef);

        _time.Advance(TimeSpan.FromSeconds(1));
        var second = await _service.GetStatusAsync(start.OrderRef);

        Assert.NotEqual(first.QrData, second.QrData);
        Assert.Contains(".1.", second.QrData);
    }

    [Fact]
    public async Task Status_NotApprovedWithin30Seconds_FailsWithStartFailed()
    {
        var start = await _service.StartAsync("127.0.0.1");

        _time.Advance(MockBankIdService.QrScanWindow);
        var status = await _service.GetStatusAsync(start.OrderRef);

        Assert.Equal("failed", status.Status);
        Assert.Equal(BankIdHintCodes.StartFailed, status.HintCode);
    }

    [Fact]
    public async Task Approve_After30Seconds_IsRejected()
    {
        var start = await _service.StartAsync("127.0.0.1");

        _time.Advance(MockBankIdService.QrScanWindow);

        Assert.False(_service.Approve(start.OrderRef, "199001011234"));
    }

    [Fact]
    public async Task Approve_Twice_SecondIsRejected()
    {
        var start = await _service.StartAsync("127.0.0.1");

        Assert.True(_service.Approve(start.OrderRef, "199001011234"));
        Assert.False(_service.Approve(start.OrderRef, "199001011234"));
    }

    [Fact]
    public async Task Status_UnknownOrder_Fails()
    {
        var status = await _service.GetStatusAsync(Guid.NewGuid().ToString());

        Assert.Equal("failed", status.Status);
    }

    [Fact]
    public async Task Cancel_RemovesTheOrder()
    {
        var start = await _service.StartAsync("127.0.0.1");

        await _service.CancelAsync(start.OrderRef);

        Assert.Equal("failed", (await _service.GetStatusAsync(start.OrderRef)).Status);
        Assert.False(_service.Approve(start.OrderRef, "199001011234"));
    }
}