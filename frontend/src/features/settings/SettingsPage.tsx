import { useSettings } from '../../hooks/useSettings'
import type { Theme } from '../../app/Context/SettingsContext'
import { useTranslation } from 'react-i18next'
import PageHeader from '../../components/ui/Pageheader'

// label är nycklar i språkfilerna
const themeOptions = [
    { value: 'light', label: 'settings.light' },
    { value: 'dark', label: 'settings.dark' },
] as const

const languageOptions = [
    { value: 'sv', label: 'language.names.sv' },
    { value: 'en', label: 'language.names.en' },
] as const

function ThemePreview({ mode }: { mode: Theme }) {
    return (
        <div className={mode}>
            <div className="flex h-24 overflow-hidden rounded-md border border-border bg-background">
                <div className="w-6 bg-sidebar" />
                <div className="flex flex-1 flex-col gap-1.5 p-2.5">
                    <div className="h-2 w-1/2 rounded bg-foreground" />
                    <div className="h-8 rounded border border-border bg-card" />
                    <div className="h-1.5 w-2/5 rounded bg-brand-text" />
                </div>
            </div>
        </div>
    )
}

export default function SettingsPage() {
    const { theme, setTheme } = useSettings()
    const { t, i18n } = useTranslation()

    return (
        <div className="flex max-w-3xl flex-col gap-7 lg:p-6">
            <PageHeader 
              title={t("settings.title")}
              subtitle={t("settings.subtitle")} />
        
            <section className="overflow-hidden md:rounded-xl border border-border bg-card">
                <div className="border-b border-border px-6 py-5">
                    <h2 className="text-large font-semibold text-foreground">{t("settings.appearance")}</h2>
                    <p className="text-small text-muted">{t("settings.appearanceHint")}</p>
                </div>

                <fieldset className="p-6">
                    <legend className="sr-only">{t("settings.theme")}</legend>
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
                                    <ThemePreview mode={option.value} />
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
                                            {t(option.label)}
                                        </span>
                                        {selected && <span className="ml-auto text-small text-brand-text">{t("settings.selected")}</span>}
                                    </div>
                                </label>
                            )
                        })}
                    </div>
                </fieldset>
            </section>

            <section className="overflow-hidden rounded-xl border border-border bg-card">
                <div className="border-b border-border px-6 py-5">
                    <h2 className="text-large font-semibold text-foreground">{t("settings.language")}</h2>
                    <p className="text-small text-muted">{t("settings.languageHint")}</p>
                </div>

                <fieldset className="p-6">
                    <legend className="sr-only">{t("settings.language")}</legend>
                    <div className="grid gap-4 sm:grid-cols-2">
                        {languageOptions.map((option) => {
                            const selected = i18n.language === option.value
                            return (
                                <label
                                    key={option.value}
                                    className={`flex cursor-pointer items-center gap-2.5 rounded-lg p-3.5 ${
                                        selected
                                            ? 'border-2 border-brand-text bg-brand-text/10'
                                            : 'border border-border bg-background'
                                    }`}
                                >
                                    <input
                                        type="radio"
                                        name="language"
                                        value={option.value}
                                        checked={selected}
                                        onChange={() => i18n.changeLanguage(option.value)}
                                        className="h-4.5 w-4.5 accent-brand-text"
                                    />
                                    <span lang={option.value} className={`text-medium text-foreground ${selected ? 'font-semibold' : ''}`}>
                                        {t(option.label)}
                                    </span>
                                    {selected && <span className="ml-auto text-small text-brand-text">{t("settings.selected")}</span>}
                                </label>
                            )
                        })}
                    </div>
                </fieldset>
            </section>

            <p className="text-small text-muted">{t("settings.savedNote")}</p>
        </div>
    )
}