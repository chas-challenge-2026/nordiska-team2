using System.Net;
using System.Net.Http.Json;

namespace NordiskaPortal.Assistant.Tests
{
    // Stands in for Ollama. Sits at the bottom of the HttpClient pipeline, so
    // AssistantService sends a real HTTP request -- it just never leaves the
    // test process. No model download, no Docker, runs anywhere (incl. CI).
    public class FakeOllamaHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _respond;

        // Every request body AssistantService sent, so tests can check what
        // actually went to the model (system prompt, examples, question).
        public List<string> RequestBodies { get; } = new();

        public FakeOllamaHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
        {
            _respond = respond;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (request.Content != null)
            {
                // Read first (await is not allowed inside a lock), then lock only
                // around the list -- concurrent requests in the busy test share it.
                var body = await request.Content.ReadAsStringAsync(ct);
                lock (RequestBodies)
                    RequestBodies.Add(body);
            }
            return await _respond(request, ct);
        }

        // --- Ready-made behaviours ---------------------------------------

        // Ollama's /api/chat response shape (only the fields the service reads).
        public static Task<HttpResponseMessage> AnswerWith(string answer) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    message = new { role = "assistant", content = answer },
                    done = true
                })
            });

        public static FakeOllamaHandler Answering(string answer) =>
            new((_, _) => AnswerWith(answer));

        // E.g. 404 = the model hasn't been pulled.
        public static FakeOllamaHandler Returning(HttpStatusCode status) =>
            new((_, _) => Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent("model not found")
            }));

        public static FakeOllamaHandler Throwing(Exception ex) =>
            new((_, _) => Task.FromException<HttpResponseMessage>(ex));
    }
}