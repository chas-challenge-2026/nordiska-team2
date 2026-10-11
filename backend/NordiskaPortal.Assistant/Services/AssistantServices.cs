using System.Diagnostics;
using Microsoft.Extensions.Options;
using NordiskaPortal.Assistant.Ollama;
using NordiskaPortal.Assistant.Options;

namespace NordiskaPortal.Assistant.Services
{
    public enum AssistantStatus { Ok, Busy, Unavailable }

    public record AssistantResult(AssistantStatus Status, string? Answer = null);

    // Asks Ollama one question at a time. Holds no state and knows nothing
    // about customers -- the only input is the question text.
    public class AssistantService
    {
        // Short, verified facts. The model summarizes these instead of guessing.
        // Keep them general -- exact amounts and tax rates change over time.
        private const string Facts =
            "FAKTA:\n" +
            "- Ett sparkonto är ett konto där pengarna ger ränta. Pengarna kan normalt tas ut när som helst. " +
            "Sparkonton omfattas av den statliga insättningsgarantin upp till ett visst belopp per person och bank.\n" +
            "- ISK betyder investeringssparkonto. Det används för att spara i aktier och fonder. " +
            "På ett ISK betalar man ingen skatt på vinster och utdelningar; i stället betalar man en årlig schablonskatt som beräknas på kontots värde. " +
            "Förluster kan inte dras av.\n" +
            "- Skillnad: ett sparkonto har ränta och låg risk; på ett ISK kan värdet både öka och minska beroende på aktier och fonder.\n" +
            "- Nordiska Sparbanken erbjuder i dag sparkonton.\n" +
            "- I internetbanken kan kunden se sina konton och saldon, sätta in och ta ut pengar, föra över pengar mellan sina egna konton, " +
            "skapa sparmål, öppna och avsluta sparkonton samt se sin aktivitetslogg.\n" +
            "- Ett konto kan bara avslutas när saldot är 0 kr. Sparmål som är kopplade till kontot tas då bort.\n" +
            "- Inloggning sker med e-post och lösenord eller med BankID.";

        // The scope, the language and the refusals all live here. The model
        // gets NO customer data and NO tools, so even a successful prompt
        // injection can at worst make it say something odd -- never reveal
        // anything or do anything.
        private const string SystemPrompt =
            "Du är Nordiska Sparbankens digitala assistent. " +
            "Svara alltid på korrekt svenska, kort och tydligt, med högst några få meningar. " +
            "Svara ENDAST utifrån FAKTA nedan. Om svaret inte finns i fakta, säg att du inte vet och hänvisa till kundtjänst. Hitta aldrig på. " +
            "Du har ingen tillgång till kundens konton, saldon eller personuppgifter. " +
            "Ge aldrig personliga investerings-, skatte- eller juridiska råd. " +
            "Om frågan inte handlar om bankärenden, säg vänligt att du bara kan hjälpa till med frågor om banken.\n\n" +
            Facts;

        // Example exchanges sent before the real question. Small models copy
        // examples far more reliably than they follow instructions.
        private static readonly OllamaMessage[] Examples =
        {
            new("user", "Skriv en dikt om katter"),
            new("assistant", "Jag kan tyvärr bara hjälpa till med frågor om banken, till exempel sparkonton, överföringar eller sparmål."),
            new("user", "Vad är mitt saldo?"),
            new("assistant", "Jag har ingen tillgång till dina konton. Ditt saldo ser du under Konton i internetbanken."),
        };

        private readonly HttpClient _http;
        private readonly OllamaGate _gate;
        private readonly AssistantOptions _options;
        private readonly ILogger<AssistantService> _logger;

        public AssistantService(
            HttpClient http,
            OllamaGate gate,
            IOptions<AssistantOptions> options,
            ILogger<AssistantService> logger)
        {
            _http = http;
            _gate = gate;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<AssistantResult> AskAsync(string question, CancellationToken ct)
        {
            if (!await _gate.TryEnterAsync(TimeSpan.FromSeconds(_options.QueueWaitSeconds), ct))
            {
                _logger.LogWarning("Assistant busy: no free Ollama slot within {Seconds}s", _options.QueueWaitSeconds);
                return new AssistantResult(AssistantStatus.Busy);
            }
            var messages = new List<OllamaMessage> { new("system", SystemPrompt) };

            var stopwatch = Stopwatch.StartNew();
            try
            {
                messages.AddRange(Examples);
                messages.Add(new("user", question));

                var request = new OllamaChatRequest(
                    _options.Model,
                    messages,
                    Stream: false,
                    new OllamaOptions(Temperature: 0.1, NumPredict: _options.MaxAnswerTokens));

                using var response = await _http.PostAsJsonAsync("api/chat", request, ct);

                if (!response.IsSuccessStatusCode)
                {
                    // E.g. 404 when the model hasn't been pulled yet.
                    var body = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogError("Ollama returned {StatusCode}: {Body}", (int)response.StatusCode, body);
                    return new AssistantResult(AssistantStatus.Unavailable);
                }

                var chat = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken: ct);
                var answer = chat?.Message?.Content?.Trim();

                if (string.IsNullOrEmpty(answer))
                {
                    _logger.LogError("Ollama returned an empty answer");
                    return new AssistantResult(AssistantStatus.Unavailable);
                }

                // Duration and size only -- never the question or the answer.
                // Customers may type personal details into a chat box.
                _logger.LogInformation("Assistant answered in {Ms} ms ({Chars} chars)",
                    stopwatch.ElapsedMilliseconds, answer.Length);

                return new AssistantResult(AssistantStatus.Ok, answer);
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                // HttpClient.Timeout expired. (If ct WAS cancelled, the caller
                // went away -- let that propagate instead.)
                _logger.LogWarning("Ollama timed out after {Seconds}s", _options.TimeoutSeconds);
                return new AssistantResult(AssistantStatus.Unavailable);
            }
            catch (HttpRequestException ex)
            {
                // Ollama not running, wrong URL, container down.
                _logger.LogError(ex, "Could not reach Ollama at {BaseUrl}", _options.BaseUrl);
                return new AssistantResult(AssistantStatus.Unavailable);
            }
            finally
            {
                _gate.Release();
            }
        }
    }
}