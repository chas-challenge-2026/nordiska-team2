import { useSettings } from '../../hooks/useSettings'

const themeOptions = [
    { value: 'light', label: 'Ljust' },
    { value: 'dark', label: 'Mörkt' },
] as const

function ThemePreview({ dark }: { dark: boolean }) {
    return (
        <div className={`flex h-24 overflow-hidden rounded-md border ${dark ? 'border-[#333a42] bg-[#121417]' : 'border-[#dddddd] bg-[#f5f5f5]'}`}>
            <div className={`w-6 ${dark ? 'bg-[#171a1f]' : 'bg-[#1a5276]'}`} />
            <div className="flex flex-1 flex-col gap-1.5 p-2.5">
                <div className={`h-2 w-1/2 rounded ${dark ? 'bg-[#e8e8e8]' : 'bg-[#333333]'}`} />
                <div className={`h-8 rounded border ${dark ? 'border-[#333a42] bg-[#1e2228]' : 'border-[#dddddd] bg-white'}`} />
                <div className={`h-1.5 w-2/5 rounded ${dark ? 'bg-[#7fb3d5]' : 'bg-[#1a5276]'}`} />
            </div>
        </div>
    )
}

export default function SettingsPage() {
    const { theme, setTheme } = useSettings()

    return (
        <div className="flex max-w-3xl flex-col gap-7 p-6">
            <header className="flex flex-col gap-1.5">
                <h1 className="text-page-title font-medium text-foreground">Inställningar</h1>
                <p className="text-medium text-muted">
                    Anpassa hur portalen ser ut och vilket språk den visas på.
                </p>
            </header>

            <section className="overflow-hidden rounded-xl border border-border bg-card">
                <div className="border-b border-border px-6 py-5">
                    <h2 className="text-large font-semibold text-foreground">Utseende</h2>
                    <p className="text-small text-muted">Välj ljust eller mörkt tema.</p>
                </div>

                <fieldset className="p-6">
                    <legend className="sr-only">Tema</legend>
                    <div className="grid gap-4 sm:grid-cols-2">
                        {themeOptions.map((option) => {
                            const selected = theme === option.value
                            return (
                                <label
                                    key={option.value}
                                    className={`flex cursor-pointer flex-col gap-3 rounded-lg p-3.5 ${
                                        selected
                                            ? 'border-2 border-brand-text bg-brand-text/10'
                                            : 'border border-border bg-background'
                                    }`}
                                >
                                    <ThemePreview dark={option.value === 'dark'} />
                                    <div className="flex items-center gap-2.5">
                                        <input
                                            type="radio"
                                            name="theme"
                                            value={option.value}
                                            checked={selected}
                                            onChange={() => setTheme(option.value)}
                                            className="h-4.5 w-4.5 accent-brand-text"
                                        />
                                        <span className={`text-medium text-foreground ${selected ? 'font-semibold' : ''}`}>
                                            {option.label}
                                        </span>
                                        {selected && <span className="ml-auto text-small text-brand-text">Valt</span>}
                                    </div>
                                </label>
                            )
                        })}
                    </div>
                </fieldset>
            </section>

            <p className="text-small text-muted">✓ Dina val sparas automatiskt på den här enheten.</p>
        </div>
    )
}