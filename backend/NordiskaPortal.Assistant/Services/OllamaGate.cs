using Microsoft.Extensions.Options;
using NordiskaPortal.Assistant.Options;

namespace NordiskaPortal.Assistant.Services
{
    // Limits how many requests talk to Ollama at the same time.
    //
    // Registered as a Singleton on purpose: the semaphore must be ONE shared
    // counter for the whole app. AssistantService is created per request
    // (typed HttpClient), so a semaphore inside it would be a new, empty
    // counter every time and limit nothing.
    //
    // This protects the server regardless of who is asking -- unlike the
    // per-customer rate limit, which only stops one customer from hogging it.
    public sealed class OllamaGate : IDisposable
    {
        private readonly SemaphoreSlim _slots;

        public OllamaGate(IOptions<AssistantOptions> options)
        {
            var max = Math.Max(1, options.Value.MaxConcurrentRequests);
            _slots = new SemaphoreSlim(max, max);
        }

        // true = got a slot (caller MUST call Release), false = timed out.
        public Task<bool> TryEnterAsync(TimeSpan wait, CancellationToken ct) => _slots.WaitAsync(wait, ct);

        public void Release() => _slots.Release();

        public void Dispose() => _slots.Dispose();
    }
}