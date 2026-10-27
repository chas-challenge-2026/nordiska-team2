using System.Collections.Concurrent;

namespace NordiskaPortal.Api.Services.BankId;

public sealed record BankIdOrder(
    string OrderRef,
    string AutoStartToken,
    string QrStartToken,
    string QrStartSecret,
    DateTimeOffset CreatedAt)
{
    // Mock only: set by the dev-only approve endpoint, standing in for the user's phone.
    public string? ApprovedPersonalId { get; init; }
}

public sealed class BankIdOrderStore
{
    // Upper bound on how long an order is remembered. For real orders BankID decides when they
    // expire (collect reports it); this only stops abandoned orders piling up in memory.
    public static readonly TimeSpan MaxOrderAge = TimeSpan.FromMinutes(5);

    private readonly ConcurrentDictionary<string, BankIdOrder> _orders = new();
    private readonly TimeProvider _time;

    public BankIdOrderStore(TimeProvider time) => _time = time;

    public void Add(BankIdOrder order)
    {
        RemoveExpired();
        _orders[order.OrderRef] = order;
    }

    public BankIdOrder? Get(string orderRef)
    {
        if (!_orders.TryGetValue(orderRef, out var order)) return null;
        if (IsExpired(order)) { _orders.TryRemove(orderRef, out _); return null; }
        return order;
    }

    // Atomic compare-and-swap: fails if the order changed in between.
    public bool TryReplace(BankIdOrder current, BankIdOrder updated) =>
        _orders.TryUpdate(current.OrderRef, updated, current);

    // Atomic: only one caller can remove a given order. This makes completion one-time use.
    public bool TryRemove(string orderRef) => _orders.TryRemove(orderRef, out _);

    private bool IsExpired(BankIdOrder order) => _time.GetUtcNow() - order.CreatedAt > MaxOrderAge;

    private void RemoveExpired()
    {
        foreach (var (key, order) in _orders)
            if (IsExpired(order)) _orders.TryRemove(key, out _);
    }
}