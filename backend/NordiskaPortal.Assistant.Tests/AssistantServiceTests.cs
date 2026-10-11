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
        public async Task Ask_SendsSystemPromptThenExamplesThenTheQuestion()
        {
            // What reaches the model IS the assistant's behaviour. If someone
            // drops the system prompt or the examples, this fails.
            var handler = FakeOllamaHandler.Answering("Svar.");
            var service = CreateService(handler);

            await service.AskAsync(Question, CancellationToken.None);

            using var body = JsonDocument.Parse(Assert.Single(handler.RequestBodies));
            var root = body.RootElement;
            var messages = root.GetProperty("messages").EnumerateArray().ToList();

            Assert.Equal("test-model", root.GetProperty("model").GetString());
            Assert.False(root.GetProperty("stream").GetBoolean());

            Assert.Equal("system", messages.First().GetProperty("role").GetString());
            Assert.Contains("FAKTA", messages.First().GetProperty("content").GetString());

            // At least one example answer between the prompt and the question.
            Assert.Contains(messages.Skip(1).SkipLast(1), m => m.GetProperty("role").GetString() == "assistant");

            Assert.Equal("user", messages.Last().GetProperty("role").GetString());
            Assert.Equal(Question, messages.Last().GetProperty("content").GetString());
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
    }
}