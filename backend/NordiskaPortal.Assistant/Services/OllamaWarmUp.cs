using System.Diagnostics;

namespace NordiskaPortal.Assistant.Services
{
    // Runs once at startup, in the background: the app starts serving right
    // away, and the first real question no longer pays ~35 s for loading the
    // model and reading both prompts.
    //
    // Retries for ~2 minutes (Ollama may still be starting), then gives up
    // quietly. A failed warm-up must never stop the app -- it only means the
    // first question is slow.
    public class OllamaWarmUp : BackgroundService
    {
        private const int MaxAttempts = 12;
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(10);

        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<OllamaWarmUp> _logger;

        public OllamaWarmUp(IServiceScopeFactory scopes, ILogger<OllamaWarmUp> logger)
        {
            _scopes = scopes;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            for (var attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                try
                {
                    // AssistantService is a typed HttpClient (transient), so
                    // resolve it in a scope instead of holding one forever.
                    using var scope = _scopes.CreateScope();
                    var assistant = scope.ServiceProvider.GetRequiredService<AssistantService>();

                    var stopwatch = Stopwatch.StartNew();
                    if (await assistant.WarmUpAsync(stoppingToken))
                    {
                        _logger.LogInformation("Ollama warm-up done in {Ms} ms", stopwatch.ElapsedMilliseconds);
                        return;
                    }
                }
                // Catch everything except "the app is shutting down" -- an
                // unhandled exception here would stop the whole host.
                catch (Exception ex) when (ex is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Ollama warm-up attempt {Attempt} failed: {Error}", attempt, ex.Message);
                }

                await Task.Delay(RetryDelay, stoppingToken);
            }

            _logger.LogWarning("Ollama warm-up gave up after {Attempts} attempts; the first question will be slow", MaxAttempts);
        }
    }
}