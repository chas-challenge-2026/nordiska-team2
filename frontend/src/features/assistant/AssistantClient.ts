import axios from "axios";

// The assistant's own HTTP client -- deliberately NOT the app's shared
// apiClient. It talks to a different service (NordiskaPortal.Assistant),
// needs no cookies, and must not trigger the banking API's refresh
// interceptor on its errors. Keeping it separate is what lets this whole
// folder be deleted without touching anything else.

const baseURL = import.meta.env.VITE_ASSISTANT_BASE_URL as string | undefined;

// No URL configured = no assistant. The widget renders nothing, so the
// feature can be switched off per environment without a code change.
export const isAssistantConfigured = Boolean(baseURL);

const assistantHttp = axios.create({
  baseURL,
  // A little longer than the server's own 60 s Ollama timeout + 5 s queue
  // wait, so the server's clean 503 arrives before the browser gives up.
  timeout: 70_000,
  headers: { "Content-Type": "application/json" },
});

// The error shapes the assistant API returns (ASP.NET ProblemDetails).
type ProblemBody = {
  detail?: string;
  errors?: Record<string, string[]>;
};

// An error with a message that's safe to show the customer as-is.
export class AssistantError extends Error {
  readonly status?: number;

  constructor(message: string, status?: number) {
    super(message);
    this.name = "AssistantError";
    this.status = status;
  }
}

export async function askAssistant(question: string, accessToken: string): Promise<string> {
  try {
    const response = await assistantHttp.post<{ answer: string }>(
      "/chat",
      { question },
      { headers: { Authorization: `Bearer ${accessToken}` } },
    );
    return response.data.answer;
  } catch (err) {
    throw toAssistantError(err);
  }
}

function toAssistantError(err: unknown): AssistantError {
  if (!axios.isAxiosError(err)) {
    return new AssistantError("Något gick fel. Försök igen.");
  }

  if (!err.response) {
    return new AssistantError(
      err.code === "ECONNABORTED"
        ? "Assistenten svarade inte i tid. Försök igen."
        : "Kunde inte nå assistenten just nu.",
    );
  }

  const status = err.response.status;
  const body = err.response.data as ProblemBody | undefined;

  switch (status) {
    case 400:
      return new AssistantError(body?.errors?.question?.[0] ?? "Frågan kunde inte skickas.", 400);
    case 401:
      return new AssistantError("Din session har gått ut. Logga in igen.", 401);
    case 429:
      return new AssistantError(
        "Du har ställt många frågor på kort tid. Vänta en minut och försök igen.",
        429,
      );
    case 503:
      // The server's own Swedish message ("upptagen" / "inte tillgänglig").
      return new AssistantError(body?.detail ?? "Assistenten är inte tillgänglig just nu.", 503);
    default:
      return new AssistantError("Något gick fel. Försök igen.", status);
  }
}