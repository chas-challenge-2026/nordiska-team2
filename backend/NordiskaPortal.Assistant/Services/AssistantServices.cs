using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using NordiskaPortal.Assistant.Ollama;
using NordiskaPortal.Assistant.Options;

namespace NordiskaPortal.Assistant.Services
{
    public enum AssistantStatus { Ok, Busy, Unavailable }

    public record AssistantResult(AssistantStatus Status, string? Answer = null);

    // Answers one question in two steps:
    //   1. CLASSIFY -- a tiny call: "is this a banking question? JA/NEJ".
    //   2. ANSWER   -- only if JA: the real call with rules, facts and examples.
    //
    // Why two steps: small models follow "only answer banking questions"
    // poorly (it wrote cake recipes even with a refusal example), but they
    // classify reliably. So the CODE returns the refusal, not the model --
    // it's always correct, always worded the same, and costs almost nothing.
    //
    // Holds no state and knows nothing about customers: the only input is
    // the question text.
    public class AssistantService
    {
        // Returned by code, never generated, when the question is off-topic.
        public const string OffTopicReply =
            "Jag kan tyvärr bara hjälpa till med frågor om banken, till exempel sparkonton, överföringar eller sparmål.";

        // --- Step 1: classification -------------------------------------

        private const string ClassifierPrompt =
            "Du är ett filter för Nordiska Sparbankens kundchatt.\n" +
            "Svara JA om meddelandet har med banken att göra. Det gäller frågor om sparande, konton, saldo, ränta, " +
            "insättningar, uttag, överföringar, sparmål, ISK, insättningsgaranti, aktivitetslogg, inloggning, BankID " +
            "och internetbanken, och även frågor om HUR man gör något, till exempel öppnar eller avslutar ett konto, " +
            "för över pengar, skapar ett sparmål eller loggar ut. Hälsningar och tack är också JA.\n" +
            "Svara NEJ bara om meddelandet tydligt handlar om något helt annat, till exempel recept, dikter, sport, " +
            "programmering eller allmänna kunskapsfrågor.\n" +
            "Om du är osäker, svara JA.\n" +
            "Svara med exakt ett ord: JA eller NEJ.";

        // Classification examples. Short on purpose: this call should be fast.
        private static readonly OllamaMessage[] ClassifierExamples =
        {
            new("user", "Ge mig ett recept på kladdkaka"),              new("assistant", "NEJ"),
            new("user", "Hur öppnar jag ett nytt konto?"),              new("assistant", "JA"),
            new("user", "Skriv en dikt om katter"),                     new("assistant", "NEJ"),
            new("user", "Kan jag skicka pengar till min kompis?"),      new("assistant", "JA"),
            new("user", "Vem vann fotbolls-VM?"),                       new("assistant", "NEJ"),
            new("user", "Vad är ett ISK?"),                             new("assistant", "JA"),
            new("user", "Jag har glömt hur man gör, kan du hjälpa?"),   new("assistant", "JA"),
            new("user", "Hej!"),                                        new("assistant", "JA"),
        };

         private static readonly Regex BankWords = new(
            @"\b(konto\w*|sparkont\w*|sparmål\w*|sparande\w*|spara|sparar|ränt\w*|överför\w*|föra\s+över|" +
            @"insättning\w*|uttag\w*|saldo\w*|isk|investeringsspar\w*|bankid|aktivitetslogg\w*|" +
            @"inloggning\w*|utloggning\w*|logga\w*|lösenord\w*|internetbank\w*)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
            TimeSpan.FromMilliseconds(100));

        public static bool HasBankKeyword(string question) => BankWords.IsMatch(question);

        // --- Step 2: the answer ------------------------------------------

        // Short, verified facts. The model summarizes these instead of
        // guessing. Keep them general -- exact amounts and rates change.
        private const string Facts =
            "FAKTA OM SPARANDE:\n" +
            "- Ett sparkonto är ett konto där pengarna ger ränta. Pengarna kan normalt tas ut när som helst. " +
            "Sparkonton omfattas av den statliga insättningsgarantin upp till ett visst belopp per person och bank.\n" +
            "- ISK betyder investeringssparkonto. Det används för att spara i aktier och fonder. " +
            "På ett ISK betalar man ingen skatt på vinster och utdelningar; i stället betalar man en årlig schablonskatt " +
            "som beräknas på kontots värde. Förluster kan inte dras av.\n" +
            "- Skillnad: ett sparkonto har ränta och låg risk; på ett ISK kan värdet både öka och minska beroende på aktier och fonder.\n" +
            "\n" +
            "FAKTA OM NORDISKA SPARBANKEN:\n" +
            "- Kunden kan själv öppna nya sparkonton i internetbanken, upp till 10 öppna konton. Nya konton får bankens standardränta.\n" +
            "- Ett konto kan bara avslutas när saldot är 0 kr. Flytta först pengarna till ett annat eget konto. " +
            "Sparmål som är kopplade till kontot tas bort när kontot avslutas.\n" +
            "- Kunden kan sätta in och ta ut pengar. Man kan inte ta ut mer än saldot.\n" +
            "- Kunden kan föra över pengar mellan sina egna konton. Överföringar till andra personer går inte att göra i internetbanken.\n" +
            "- Till insättningar, uttag och överföringar kan man skriva en egen beskrivning på högst 100 tecken.\n" +
            "- Sparmål: kunden kan skapa sparmål med namn, målbelopp och valfritt slutdatum, och koppla dem till ett konto. " +
            "Då visas hur långt man har kommit utifrån kontots saldo.\n" +
            "- Aktivitetsloggen visar kundens egna händelser: inloggningar, misslyckade inloggningsförsök, insättningar, uttag, " +
            "överföringar, öppnade och avslutade konton samt sparmål. Ser kunden inloggningsförsök som inte är deras egna " +
            "ska de kontakta kundtjänst.\n" +
            "- Inloggning sker med e-post och lösenord eller med BankID. Logga alltid ut när du är klar, särskilt på en delad dator.\n" +
            "- Gör man många försök på kort tid kan man behöva vänta en minut innan man kan försöka igen.\n" +
            "\n" +
            "OM ASSISTENTEN:\n" +
            "- Assistenten är en AI och kan svara fel. Den ser inte kundens konton.\n" +
            "- Skriv aldrig lösenord, personnummer eller kortuppgifter i chatten.";

        // How-to answers, phrased like the questions customers ask. General
        // on purpose: no menu names or button labels we haven't confirmed.
        private const string HowToFacts =
            "SÅ GÖR MAN I INTERNETBANKEN:\n" +
            "- Öppna ett sparkonto: kunden öppnar själv ett nytt sparkonto i internetbanken, utan att kontakta banken. " +
            "Kontot är tomt från början och får bankens standardränta och ett kontonummer från banken. Man kan ha upp till 10 öppna konton.\n" +
            "- Avsluta ett konto: flytta först hela saldot till ett annat eget konto så att saldot blir 0 kr, och avsluta sedan kontot i internetbanken. " +
            "Sparmål som är kopplade till kontot tas bort.\n" +
            "- Föra över pengar: det går att föra över mellan sina egna konton. Överföringar till andra personer går inte att göra i internetbanken.\n" +
            "- Sätta in och ta ut pengar: man kan inte ta ut mer än saldot. Man kan skriva en egen beskrivning på högst 100 tecken.\n" +
            "- Skapa ett sparmål: ange namn, målbelopp och gärna ett slutdatum, och koppla det till ett konto för att följa hur det går.\n" +
            "- Aktivitetsloggen visar inloggningar, utloggningar, insättningar, uttag, överföringar, öppnade och avslutade konton samt sparmål. " +
            "Ser man en inloggning man inte känner igen ska man kontakta kundtjänst.\n" +
            "- Logga in med e-post och lösenord eller med BankID. Logga alltid ut på en delad dator.";

        // One rule per line: small models follow separate lines far better
        // than one long sentence. Rules first, facts last, nothing after
        // the facts (anything appended there is read as another fact).
        //
        // The model gets NO customer data and NO tools, so even a successful
        // prompt injection can at worst make it say something odd.
        private const string SystemPrompt =
            "Du är Nordiska Sparbankens digitala assistent.\n" +
            "REGLER:\n" +
            "- Svara på korrekt svenska, kort och tydligt, med högst tre meningar.\n" +
            "- Svara endast utifrån FAKTA nedan. Om svaret inte finns där, säg att du inte vet och hänvisa till kundtjänst.\n" +
            "- Hitta aldrig på siffror, regler eller tjänster.\n" +
            "- Du har ingen tillgång till kundens konton, saldon eller personuppgifter.\n" +
            "- Ge aldrig personliga investerings-, skatte- eller juridiska råd.\n\n" +
            Facts + "\n\n" + HowToFacts;

        private static readonly OllamaMessage[] AnswerExamples =
        {
            new("user", "Vad är mitt saldo?"),
            new("assistant", "Jag har ingen tillgång till dina konton. Ditt saldo ser du under Konton i internetbanken."),
            new("user", "Ska jag köpa aktier i år?"),
            new("assistant", "Jag kan inte ge personliga investeringsråd. Kontakta gärna kundtjänst om du vill ha rådgivning."),
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

            // Both calls run inside the same slot, so one question never
            // counts as two against the concurrency limit.
            var stopwatch = Stopwatch.StartNew();
            try
            {
                // --- Step 1: classify ---
                if (!HasBankKeyword(question))
                {
                    var verdict = await ChatAsync(ClassifierPrompt, ClassifierExamples, question,
                        temperature: 0, maxTokens: 3, ct);

                    if (verdict is null)
                        return new AssistantResult(AssistantStatus.Unavailable);

                    // Only an explicit NEJ blocks.
                    if (verdict.Trim().ToUpperInvariant().StartsWith("NEJ"))
                    {
                        _logger.LogInformation("Assistant refused an off-topic question in {Ms} ms", stopwatch.ElapsedMilliseconds);
                        return new AssistantResult(AssistantStatus.Ok, OffTopicReply);
                    }
                }

                // --- Step 2: answer ---
                var answer = await ChatAsync(SystemPrompt, AnswerExamples, question,
                    temperature: 0.1, maxTokens: _options.MaxAnswerTokens, ct);

                if (string.IsNullOrWhiteSpace(answer))
                {
                    _logger.LogError("Ollama returned an empty answer");
                    return new AssistantResult(AssistantStatus.Unavailable);
                }

                // Duration and size only -- never the question or the answer.
                _logger.LogInformation("Assistant answered in {Ms} ms ({Chars} chars)",
                    stopwatch.ElapsedMilliseconds, answer.Length);

                return new AssistantResult(AssistantStatus.Ok, answer.Trim());
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
                _logger.LogError(ex, "Could not reach Ollama at {BaseUrl}", _options.BaseUrl);
                return new AssistantResult(AssistantStatus.Unavailable);
            }
            finally
            {
                _gate.Release();
            }
        }

        // One /api/chat call. Returns the reply text, "" for an empty reply,
        // or null if Ollama answered with an error status.
        private async Task<string?> ChatAsync(
            string systemPrompt,
            IEnumerable<OllamaMessage> examples,
            string question,
            double temperature,
            int maxTokens,
            CancellationToken ct)
        {
            var messages = new List<OllamaMessage> { new("system", systemPrompt) };
            messages.AddRange(examples);
            messages.Add(new("user", question));

            var request = new OllamaChatRequest(
                _options.Model,
                messages,
                Stream: false,
                KeepAlive: _options.KeepAlive,
                new OllamaOptions(temperature, maxTokens, _options.ContextSize));

            using var response = await _http.PostAsJsonAsync("api/chat", request, ct);

            if (!response.IsSuccessStatusCode)
            {
                // E.g. 404 when the model hasn't been pulled yet.
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Ollama returned {StatusCode}: {Body}", (int)response.StatusCode, body);
                return null;
            }

            var chat = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken: ct);
            return chat?.Message?.Content ?? "";
        }
    }
}