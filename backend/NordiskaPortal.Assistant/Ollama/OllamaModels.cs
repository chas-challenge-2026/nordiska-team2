using System.Text.Json.Serialization;

namespace NordiskaPortal.Assistant.Ollama
{
    // The parts of Ollama's /api/chat contract this service uses.
    // Unknown fields in the response are ignored, so Ollama adding fields
    // later doesn't break anything.

    public record OllamaChatRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyList<OllamaMessage> Messages,
        [property: JsonPropertyName("stream")] bool Stream,
        // How long Ollama keeps the model in memory after this request.
        // Unloading and reloading costs 10-20 s on a CPU.
        [property: JsonPropertyName("keep_alive")] string KeepAlive,
        [property: JsonPropertyName("options")] OllamaOptions Options);

    public record OllamaMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    public record OllamaOptions(
        [property: JsonPropertyName("temperature")] double Temperature,
        [property: JsonPropertyName("num_predict")] int NumPredict,
        // Context window in tokens. The prompt + question fit in ~1000;
        // a smaller window is lighter on CPU and memory.
        [property: JsonPropertyName("num_ctx")] int NumCtx,
        // Left out of the JSON when null, so Ollama picks the thread count itself.
        [property: JsonPropertyName("num_thread"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? NumThread = null);

    public record OllamaChatResponse(
        [property: JsonPropertyName("message")] OllamaMessage? Message,
        [property: JsonPropertyName("done")] bool Done,
        [property: JsonPropertyName("done_reason")] string? DoneReason = null,
        [property: JsonPropertyName("total_duration")] long TotalDuration = 0,
        [property: JsonPropertyName("load_duration")] long LoadDuration = 0,
        [property: JsonPropertyName("prompt_eval_count")] int PromptEvalCount = 0,
        [property: JsonPropertyName("prompt_eval_duration")] long PromptEvalDuration = 0,
        [property: JsonPropertyName("eval_count")] int EvalCount = 0,
        [property: JsonPropertyName("eval_duration")] long EvalDuration = 0);
}