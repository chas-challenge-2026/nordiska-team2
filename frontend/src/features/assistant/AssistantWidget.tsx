import { useEffect, useRef, useState, type FormEvent, type KeyboardEvent } from "react";
import { askAssistant, AssistantError, isAssistantConfigured } from "./AssistantClient";

// A floating chat bubble, bottom right.
//
// Independent on purpose: it imports nothing from the rest of the app.
// Everything it needs comes in as props from the single place it's mounted,
// so deleting src/features/assistant/ and that one line removes it entirely.

const MAX_LENGTH = 500; // matches the server's Assistant:MaxQuestionLength

// The assistant gets a random name each session, shown as "AI-<name>".
// The "AI-" prefix is deliberate: the customer must always be able to tell
// they're talking to an AI, not a person.
// Anna and Erik are left out on purpose -- they're the seeded test
// customers, and "AI-Anna" answering Anna would be confusing.
const ASSISTANT_NAMES = [
  "Alice", "Bob", "Elsa", "Hugo", "Maja", "Oskar",
  "Saga", "Viktor", "Ebba", "Liam", "Freja", "Nils",
] as const;

function pickAssistantName(): string {
  const name = ASSISTANT_NAMES[Math.floor(Math.random() * ASSISTANT_NAMES.length)];
  return `AI-${name}`;
}

type Message = {
  id: number;
  role: "user" | "assistant" | "error";
  text: string;
};

type Props = {
  // The banking API's access token. null = not logged in = no widget.
  accessToken: string | null;
  // Optional: gets a fresh token when the current one has expired, so a
  // customer who has only been chatting for 15+ minutes isn't logged out.
  refreshToken?: () => Promise<string>;
};

export function AssistantWidget({ accessToken, refreshToken }: Props) {
  const [open, setOpen] = useState(false);
  // Kept in memory only, never localStorage: customers may type personal
  // details. It's gone on reload, and on logout (the widget unmounts), so
  // the next person on the same computer never sees the previous chat.
  const [messages, setMessages] = useState<Message[]>([]);
  const [input, setInput] = useState("");
  const [sending, setSending] = useState(false);
  const [assistantName] = useState(pickAssistantName);

  const nextId = useRef(0);
  const inputRef = useRef<HTMLTextAreaElement>(null);
  const toggleRef = useRef<HTMLButtonElement>(null);
  const endRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (open) inputRef.current?.focus();
  }, [open]);

  useEffect(() => {
    endRef.current?.scrollIntoView({ behavior: "smooth", block: "end" });
  }, [messages, sending]);

  if (!isAssistantConfigured || !accessToken) return null;

  const token = accessToken;

  function add(role: Message["role"], text: string) {
    setMessages((prev) => [...prev, { id: nextId.current++, role, text }]);
  }

  function close() {
    setOpen(false);
    toggleRef.current?.focus(); // return focus to where the user was
  }

  // One retry after a 401: refresh the token once, then ask again.
  async function askWithRefresh(question: string): Promise<string> {
    try {
      return await askAssistant(question, token);
    } catch (err) {
      if (err instanceof AssistantError && err.status === 401 && refreshToken) {
        const fresh = await refreshToken().catch(() => null);
        if (fresh) return askAssistant(question, fresh);
      }
      throw err;
    }
  }

  async function send(event?: FormEvent) {
    event?.preventDefault();
    const question = input.trim();
    if (!question || sending) return;

    add("user", question);
    setInput("");
    setSending(true);

    try {
      add("assistant", await askWithRefresh(question));
    } catch (err) {
      add("error", err instanceof AssistantError ? err.message : "Något gick fel. Försök igen.");
    } finally {
      setSending(false);
      inputRef.current?.focus();
    }
  }

  // Enter sends, Shift+Enter makes a new line.
  function onInputKeyDown(event: KeyboardEvent<HTMLTextAreaElement>) {
    if (event.key === "Enter" && !event.shiftKey) {
      event.preventDefault();
      void send();
    }
  }

  function onPanelKeyDown(event: KeyboardEvent<HTMLElement>) {
    if (event.key === "Escape") close();
  }

  return (
    <div className="fixed bottom-[calc(5rem+env(safe-area-inset-bottom))] right-4 z-50 flex flex-col items-end gap-3 md:bottom-4">
      {open && (
        <section
          role="dialog"
          aria-label={`Chatt med ${assistantName}, Nordiska Sparbankens AI-assistent`}
          onKeyDown={onPanelKeyDown}
          className="flex h-[min(32rem,calc(100dvh-11rem))] md:h-[min(32rem,calc(100dvh-6rem))] w-[min(22rem,calc(100vw-2rem))] flex-col overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-2xl"
        >
          <header className="flex items-center justify-between bg-[#1A5276] px-4 py-3 text-white">
            <div>
              <h2 className="text-sm font-semibold">{assistantName}</h2>
              <p className="text-xs text-slate-300">AI-svar kan innehålla fel</p>
            </div>
            <button
              type="button"
              onClick={close}
              aria-label="Stäng chatten"
              className="rounded p-1 hover:bg-white/10 focus:outline-none focus-visible:ring-2 focus-visible:ring-white"
            >
              <CloseIcon />
            </button>
          </header>

          {/* aria-live: screen readers announce new answers as they arrive */}
          <div className="flex-1 space-y-3 overflow-y-auto p-4 text-sm" aria-live="polite">
            {messages.length === 0 && (
              <p className="text-slate-500">
                Hej! Jag är {assistantName}. Jag svarar på allmänna frågor om sparande och internetbanken.
              </p>
            )}

            {messages.map((m) => (
              <div
                key={m.id}
                className={
                  m.role === "user"
                    ? "ml-8 rounded-2xl rounded-br-sm bg-[#1A5276] px-3 py-2 text-white"
                    : m.role === "assistant"
                      ? "mr-8 rounded-2xl rounded-bl-sm bg-slate-100 px-3 py-2 text-slate-900"
                      : "mr-8 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-red-700"
                }
              >
                {/* Plain text only and not dangerouslySetInnerHTML. The
                    answer comes from a language model and the question
                    from the user so neither may ever be rendered as HTML. */}
                <p className="whitespace-pre-wrap break-words">{m.text}</p>
              </div>
            ))}

            {sending && <p className="italic text-slate-500">{assistantName} funderar…</p>}
            <div ref={endRef} />
          </div>

          <form onSubmit={send} className="border-t border-slate-200 p-3">
            <textarea
              ref={inputRef}
              value={input}
              onChange={(e) => setInput(e.target.value)}
              onKeyDown={onInputKeyDown}
              maxLength={MAX_LENGTH}
              rows={2}
              placeholder="Skriv din fråga…"
              aria-label="Din fråga"
              className="w-full resize-none rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-slate-500 focus:outline-none"
            />
            <div className="mt-2 flex items-center justify-between">
              <span className="text-xs text-slate-400">
                {input.length}/{MAX_LENGTH}
              </span>
              <button
                type="submit"
                disabled={sending || !input.trim()}
                className="rounded-lg bg-[#1A5276] px-4 py-1.5 text-sm font-medium text-white hover:bg-slate-700 disabled:cursor-not-allowed disabled:opacity-40"
              >
                Skicka
              </button>
            </div>
          </form>
        </section>
      )}

      <button
        ref={toggleRef}
        type="button"
        onClick={() => (open ? close() : setOpen(true))}
        aria-expanded={open}
        aria-label={open ? "Stäng chatten" : `Öppna chatt med ${assistantName}`}  
        className="flex h-12 w-12 md:h-14 md:w-14 items-center justify-center rounded-full bg-[#1A5276] text-white shadow-lg hover:bg-slate-700 focus:outline-none focus-visible:ring-4 focus-visible:ring-slate-400"
      >
        {open ? <CloseIcon /> : <ChatIcon />}
      </button>
    </div>
  );
}

function ChatIcon() {
  return (
    <svg className="h-5 w-5 md:h-6 md:w-6" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z" />
    </svg>
  );
}

function CloseIcon() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" aria-hidden="true">
      <path d="M18 6 6 18M6 6l12 12" />
    </svg>
  );
}