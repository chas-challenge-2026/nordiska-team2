using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NordiskaPortal.Assistant.Contracts;
using NordiskaPortal.Assistant.Services;
using Xunit;

namespace NordiskaPortal.Assistant.Tests
{
    // The whole HTTP pipeline, in memory: authentication, rate limiting,
    // validation, status codes. Only Ollama is fake.
    public class AssistantApiFactory : WebApplicationFactory<Program>
    {
        public const string JwtKey = "test-signing-key-at-least-32-bytes-long-for-hmacsha256";
        public const string Issuer = "NordiskaPortal.Tests";
        public const string Audience = "NordiskaPortal.Tests.Client";

        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _ollama;

        public AssistantApiFactory(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> ollama)
        {
            _ollama = ollama;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // "Testing", not Development: no User Secrets get loaded, so the
            // test never depends on what's configured on this machine.
            builder.UseEnvironment("Testing");

            // UseSetting reaches the app before Program.cs reads Jwt:Key, so
            // no fallback key is needed in Program.cs.
            builder.UseSetting("Jwt:Key", JwtKey);
            builder.UseSetting("Jwt:Issuer", Issuer);
            builder.UseSetting("Jwt:Audience", Audience);

            builder.ConfigureTestServices(services =>
            {
                services.AddHttpClient<AssistantService>()
                    .ConfigurePrimaryHttpMessageHandler(() => new FakeOllamaHandler(_ollama));
            });
        }

        public static string CreateToken(string customerId, string signingKey = JwtKey)
        {
            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: Issuer,
                audience: Audience,
                claims: new[] { new Claim(JwtRegisteredClaimNames.Sub, customerId) },
                expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class ChatEndpointTests
    {
        private static HttpClient ClientWithToken(AssistantApiFactory factory, string token)
        {
            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        // A fresh customer id per test, so the per-customer rate limit of one
        // test never affects another.
        private static string NewCustomerId() => Random.Shared.Next(100_000, 999_999).ToString();

        private static Task<HttpResponseMessage> Ask(HttpClient client, string? question) =>
            client.PostAsJsonAsync("/api/assistant/chat", new { question });

        [Fact]
        public async Task Chat_WithValidQuestion_Returns200WithTheAnswer()
        {
            using var factory = new AssistantApiFactory((_, _) => FakeOllamaHandler.AnswerWith("Ett sparkonto ger ränta."));
            var client = ClientWithToken(factory, AssistantApiFactory.CreateToken(NewCustomerId()));

            var response = await Ask(client, "Vad är ett sparkonto?");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<ChatResponse>();
            Assert.Equal("Ett sparkonto ger ränta.", body!.Answer);
        }

        [Fact]
        public async Task Chat_WithoutToken_Returns401()
        {
            using var factory = new AssistantApiFactory((_, _) => FakeOllamaHandler.AnswerWith("x"));

            var response = await Ask(factory.CreateClient(), "Vad är ett sparkonto?");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Chat_WithTokenSignedByAnotherKey_Returns401()
        {
            // Proves the signature is really checked, not just "any token".
            using var factory = new AssistantApiFactory((_, _) => FakeOllamaHandler.AnswerWith("x"));
            var forged = AssistantApiFactory.CreateToken(NewCustomerId(), "a-completely-different-key-32-bytes-long!!");

            var response = await Ask(ClientWithToken(factory, forged), "Vad är ett sparkonto?");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task Chat_WithEmptyQuestion_Returns400(string? question)
        {
            using var factory = new AssistantApiFactory((_, _) => FakeOllamaHandler.AnswerWith("x"));
            var client = ClientWithToken(factory, AssistantApiFactory.CreateToken(NewCustomerId()));

            var response = await Ask(client, question);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Chat_WithTooLongQuestion_Returns400()
        {
            using var factory = new AssistantApiFactory((_, _) => FakeOllamaHandler.AnswerWith("x"));
            var client = ClientWithToken(factory, AssistantApiFactory.CreateToken(NewCustomerId()));

            var response = await Ask(client, new string('a', 501));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Chat_WithUnreadableJson_Returns400_Not500()
        {
            // Regression test for the PowerShell/Latin-1 finding: a body the
            // server can't read is the client's fault.
            using var factory = new AssistantApiFactory((_, _) => FakeOllamaHandler.AnswerWith("x"));
            var client = ClientWithToken(factory, AssistantApiFactory.CreateToken(NewCustomerId()));

            var response = await client.PostAsync("/api/assistant/chat",
                new StringContent("{ not json", Encoding.UTF8, "application/json"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Chat_WhenOllamaIsDown_Returns503WithASwedishMessage()
        {
            using var factory = new AssistantApiFactory((_, _) =>
                Task.FromException<HttpResponseMessage>(new HttpRequestException("connection refused")));
            var client = ClientWithToken(factory, AssistantApiFactory.CreateToken(NewCustomerId()));

            var response = await Ask(client, "Vad är ett sparkonto?");

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Contains("inte tillgänglig", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Chat_SixthRequestWithinAMinute_Returns429()
        {
            using var factory = new AssistantApiFactory((_, _) => FakeOllamaHandler.AnswerWith("Svar."));
            var client = ClientWithToken(factory, AssistantApiFactory.CreateToken(NewCustomerId()));

            for (var i = 0; i < 5; i++)
                Assert.Equal(HttpStatusCode.OK, (await Ask(client, "Fråga")).StatusCode);

            Assert.Equal(HttpStatusCode.TooManyRequests, (await Ask(client, "Fråga")).StatusCode);
        }

        [Fact]
        public async Task Health_IsOpenWithoutToken()
        {
            using var factory = new AssistantApiFactory((_, _) => FakeOllamaHandler.AnswerWith("x"));

            var response = await factory.CreateClient().GetAsync("/health");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}