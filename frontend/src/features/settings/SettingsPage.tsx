import { useSettings } from '../../hooks/useSettings'

export default function SettingsPage() {
    const { theme, setTheme } = useSettings()

    return (
    <div className="mx-auto max-w-2xl p-6">
        <h1 className="mb-6 text-page-title font-semibold text-foreground">Inställningar</h1>

        <section className="rounded-default border border-border bg-card p-6">
            <h2 className="mb-4 text-large font-medium text-foreground">Utseende</h2>
            <div className="flex gap-3">
                <button
                    onClick={() => setTheme('light')}
                    className={`rounded-default border px-4 py-2 ${
                        theme === 'light'
                            ? 'border-brand bg-brand text-white'
                            : 'border-border text-foreground'
                    }`}
                >
                    ☀️ Ljust
                </button>
                <button
                    onClick={() => setTheme('dark')}
                    className={`rounded-default border px-4 py-2 ${
                        theme === 'dark'
                            ? 'border-brand bg-brand text-white'
                            : 'border-border text-foreground'
                    }`}
                >
                    🌙 Mörkt
                </button>
            </div>
        </section>
    </div>
)
}