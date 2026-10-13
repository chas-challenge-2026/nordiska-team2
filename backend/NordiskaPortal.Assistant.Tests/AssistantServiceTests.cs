using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using NordiskaPortal.Assistant.Options;
using NordiskaPortal.Assistant.Services;
using Xunit;

namespace NordiskaPortal.Assistant.Tests
{
    // AssistantService on its own: how it talks to Ollama and how it turns
    // every kind of failure into a clean status instead of an exception.
    public class AssistantServiceTests
    {
        private const string Question = "Vad är ett sparkonto?";

        // No bank keywords, so it goes through the classifier.
        private const string PlainQuestion = "Kan ni hjälpa mig med en sak?";

        private static AssistantService CreateService(
            HttpMessageHandler handler,
            Action<AssistantOptions>? configure = null)
        {
            var options = new AssistantOptions
            {
                Model = "test-model",
                MaxConcurrentRequests = 2,
                QueueWaitSeconds = 1,
                MaxAnswerTokens = 100
            };
            configure?.Invoke(options);

            // Fully qualified: inside this namespace, plain "Options" would
            // resolve to the NordiskaPortal.Assistant.Options namespace.
            var wrapped = Microsoft.Extensions.Options.Options.Create(options);

            var http = new HttpClient(handler) { BaseAddress = new Uri("http://fake-ollama/") };
            return new AssistantService(http, new OllamaGate(wrapped), wrapped, NullLogger<AssistantService>.Instance);
        }

        [Fact]
        public async Task Ask_WhenOllamaAnswers_ReturnsTheTrimmedAnswer()
        {
            var service = CreateService(FakeOllamaHandler.Answering("  Ett sparkonto ger ränta.  "));

            var result = await service.AskAsync(Question, CancellationToken.None);

            Assert.Equal(AssistantStatus.Ok, result.Status);
            Assert.Equal("Ett sparkonto ger ränta.", result.Answer);
        }

        [Fact]
        public async Task Ask_ClassifiesFirst_ThenSendsSystemPromptExamplesAndQuestion()
        {
            var handler = FakeOllamaHandler.Answering("Svar.");
            var service = CreateService(handler);
        
            await service.AskAsync(PlainQuestion, CancellationToken.None);
        
            // Two calls: the classification, then the answer.
            Assert.Equal(2, handler.RequestBodies.Count);
            Assert.Contains("JA eller NEJ", handler.RequestBodies[0]);
        
            using var body = JsonDocument.Parse(handler.RequestBodies[1]);
            var root = body.RootElement;
            var messages = root.GetProperty("messages").EnumerateArray().ToList();
        
            Assert.Equal("test-model", root.GetProperty("model").GetString());
            Assert.False(root.GetProperty("stream").GetBoolean());
        
            Assert.Equal("system", messages.First().GetProperty("role").GetString());
            Assert.Contains("FAKTA", messages.First().GetProperty("content").GetString());
            Assert.Contains(messages.Skip(1).SkipLast(1), m => m.GetProperty("role").GetString() == "assistant");
        
            Assert.Equal("user", messages.Last().GetProperty("role").GetString());
            Assert.Equal(PlainQuestion, messages.Last().GetProperty("content").GetString());
        }
        
        // A fake Ollama that answers the classification call with `verdict`
        // and the answer call with `answer` -- told apart by the classifier
        // prompt's "JA eller NEJ".
        private static FakeOllamaHandler Classifying(string verdict, string answer) =>
            new(async (request, ct) =>
            {
                var body = await request.Content!.ReadAsStringAsync(ct);
                return await FakeOllamaHandler.AnswerWith(body.Contains("JA eller NEJ") ? verdict : answer);
            });
        
        [Fact]
        public async Task Ask_WhenClassifiedOffTopic_ReturnsTheFixedRefusal_AndNeverAsksForAnAnswer()
        {
            // The cake-recipe case: the refusal comes from code, so the model
            // never gets the chance to write the recipe.
            var handler = Classifying("NEJ", "Här är ett recept på kladdkaka...");
            var service = CreateService(handler);
        
            var result = await service.AskAsync("Ge mig ett recept på kladdkaka", CancellationToken.None);
        
            Assert.Equal(AssistantStatus.Ok, result.Status);
            Assert.Equal(AssistantService.OffTopicReply, result.Answer);
            Assert.Single(handler.RequestBodies);   // only the classification call was made
        }
        
        [Fact]
        public async Task Ask_WhenClassifiedAsBanking_ReturnsTheModelsAnswer()
        {
            var handler = Classifying("JA", "Ett sparkonto ger ränta.");
            var service = CreateService(handler);
        
            var result = await service.AskAsync(PlainQuestion, CancellationToken.None);
        
            Assert.Equal(AssistantStatus.Ok, result.Status);
            Assert.Equal("Ett sparkonto ger ränta.", result.Answer);
            Assert.Equal(2, handler.RequestBodies.Count);
        }

        [Fact]
        public async Task Ask_WhenOllamaReturnsAnError_IsUnavailable()
        {
            var service = CreateService(FakeOllamaHandler.Returning(HttpStatusCode.NotFound));

            var result = await service.AskAsync(Question, CancellationToken.None);

            Assert.Equal(AssistantStatus.Unavailable, result.Status);
        }

        [Fact]
        public async Task Ask_WhenOllamaTimesOut_IsUnavailable()
        {
            // HttpClient signals its own timeout as TaskCanceledException
            // while the caller's token is NOT cancelled.
            var service = CreateService(FakeOllamaHandler.Throwing(new TaskCanceledException("timeout")));

            var result = await service.AskAsync(Question, CancellationToken.None);

            Assert.Equal(AssistantStatus.Unavailable, result.Status);
        }

        [Fact]
        public async Task Ask_WhenOllamaIsUnreachable_IsUnavailable()
        {
            var service = CreateService(FakeOllamaHandler.Throwing(new HttpRequestException("connection refused")));

            var result = await service.AskAsync(Question, CancellationToken.None);

            Assert.Equal(AssistantStatus.Unavailable, result.Status);
        }

        [Fact]
        public async Task Ask_WhenOllamaAnswersEmpty_IsUnavailable()
        {
            var service = CreateService(FakeOllamaHandler.Answering("   "));

            var result = await service.AskAsync(Question, CancellationToken.None);

            Assert.Equal(AssistantStatus.Unavailable, result.Status);
        }

        [Fact]
        public async Task Ask_WhenAllSlotsAreTaken_IsBusy_AndTheFirstRequestStillCompletes()
        {
            // One slot. The first request parks inside "Ollama" until released.
            var firstEntered = new TaskCompletionSource();
            var releaseFirst = new TaskCompletionSource();

            var handler = new FakeOllamaHandler(async (_, _) =>
            {
                firstEntered.TrySetResult();
                await releaseFirst.Task;
                return await FakeOllamaHandler.AnswerWith("Klart.");
            });

            var service = CreateService(handler, o =>
            {
                o.MaxConcurrentRequests = 1;
                o.QueueWaitSeconds = 0;     // don't wait at all for a slot
            });

            var first = service.AskAsync(Question, CancellationToken.None);
            await firstEntered.Task;        // the first request now holds the only slot

            var second = await service.AskAsync(Question, CancellationToken.None);
            Assert.Equal(AssistantStatus.Busy, second.Status);

            releaseFirst.SetResult();
            Assert.Equal(AssistantStatus.Ok, (await first).Status);
        }

        [Fact]
        public async Task Ask_ReleasesItsSlotEvenWhenOllamaFails()
        {
            // One slot, no waiting. If a failed request kept its slot, the
            // second request would come back Busy -- and after
            // MaxConcurrentRequests failures the assistant would be busy forever.
            var service = CreateService(
                FakeOllamaHandler.Throwing(new HttpRequestException("down")),
                o => { o.MaxConcurrentRequests = 1; o.QueueWaitSeconds = 0; });

            var first = await service.AskAsync(Question, CancellationToken.None);
            var second = await service.AskAsync(Question, CancellationToken.None);

            Assert.Equal(AssistantStatus.Unavailable, first.Status);
            Assert.Equal(AssistantStatus.Unavailable, second.Status);   // not Busy
        }

        [Fact]
        public async Task Ask_WhenTheCallerCancels_ThrowsInsteadOfReportingUnavailable()
        {
            // A client that disconnects is not "Ollama is down" -- it should
            // not be logged or counted as an outage.
            var service = CreateService(FakeOllamaHandler.Answering("Svar."));
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.AskAsync(Question, cts.Token));
        }

        [Fact]
        public async Task Ask_WithBankWords_SkipsTheClassifier_EvenIfItWouldSayNej()
        {
            var handler = Classifying("NEJ", "Du öppnar ett sparkonto i internetbanken.");
            var service = CreateService(handler);

            var result = await service.AskAsync("Hur öppnar jag ett sparkonto?", CancellationToken.None);

            Assert.Equal("Du öppnar ett sparkonto i internetbanken.", result.Answer);
            var body = Assert.Single(handler.RequestBodies);
            Assert.DoesNotContain("JA eller NEJ", body);
        }

        [Fact]
        public async Task WarmUp_SendsBothPrompts_SoBothGetCached()
        {
            var handler = FakeOllamaHandler.Answering("x");
            var service = CreateService(handler);

            Assert.True(await service.WarmUpAsync(CancellationToken.None));

            Assert.Equal(2, handler.RequestBodies.Count);
            Assert.Contains("JA eller NEJ", handler.RequestBodies[0]);
            Assert.Contains("FAKTA", handler.RequestBodies[1]);
        }

        [Theory]
        [InlineData("Hur öppnar jag ett sparkonto?", true)]
        [InlineData("Hur avslutar jag ett konto?", true)]
        [InlineData("Vad visar aktivitetsloggen?", true)]
        [InlineData("Hur loggar jag ut?", true)]
        [InlineData("Kan jag föra över pengar till någon annan?", true)]
        [InlineData("Ge mig ett recept på fisk", false)]   // "isk" inside a word
        [InlineData("Recept på sparris", false)]           // "spar" inside a word
        [InlineData("Skriv en dikt om katter", false)]
        public void HasBankKeyword_MatchesWholeBankWordsOnly(string question, bool expected) =>
            Assert.Equal(expected, AssistantService.HasBankKeyword(question));
    }
}