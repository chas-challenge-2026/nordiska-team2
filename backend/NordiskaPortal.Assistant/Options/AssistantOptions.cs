namespace NordiskaPortal.Assistant.Options
{
    // Bound from the "Assistant" section in appsettings.json. Everything that
    // might need tuning lives here, so switching model or limits is a config
    // change, never a code change.
    public class AssistantOptions
    {
        public const string SectionName = "Assistant";

        // Where Ollama listens. Locally http://localhost:11434, in Docker
        // Compose http://ollama:11434 (the service name on the internal network).
        public string BaseUrl { get; set; } = "http://localhost:11434";

        public string Model { get; set; } = "qwen2.5:3b";

        // The first request after a while is slow: Ollama loads the model into
        // memory. 60 s covers that on a laptop CPU.
        public int TimeoutSeconds { get; set; } = 60;

        // Loaded time after a request ("30m", "-1" = forever).
        public string KeepAlive { get; set; } = "30m";

        // How many questions Ollama works on at once. More than this and the
        // CPU just gets slower for everyone -- extra requests wait briefly,
        // then get "busy".
        public int MaxConcurrentRequests { get; set; } = 2;

        // How long a request waits for a free slot before giving up.
        public int QueueWaitSeconds { get; set; } = 5;

        public int MaxQuestionLength { get; set; } = 500;

        // Caps answer length (Ollama's num_predict) -- and with it, cost per request.
        public int MaxAnswerTokens { get; set; } = 200;

        // Context window in tokens. The prompt + examples + question fit well within this.
        public int ContextSize { get; set; } = 2048;

        // Threads Ollama computes with. Should equal the container's CPU limit
        // (OLLAMA_CPUS): more threads than allowed CPUs get throttled.
        // null = let Ollama decide (fine for a local `ollama serve`).
        public int? Threads { get; set; }
    }
}