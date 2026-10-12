import { useTranslation } from "react-i18next"

const languages = [
    { code: "sv", label: "SV" },
    { code: "en", label: "EN" },
]

export default function LanguageSwitcher({ className = "" }: { className?: string }) {
    const { t, i18n } = useTranslation()

    return (
        <div
            role="group"
            aria-label={t("language.choose")}
            className={`flex gap-1 rounded-full border border-border bg-border p-1 text-xsmall ${className}`}
        >
            {languages.map((lang) => {
                const isActive = i18n.language === lang.code
                return (
                    <button
                        key={lang.code}
                        type="button"
                        lang={lang.code}
                        aria-pressed={isActive}
                        aria-label={t(`language.names.${lang.code}`)}
                        onClick={() => i18n.changeLanguage(lang.code)}
                        className={`cursor-pointer rounded-full px-3 py-1 transition-colors ${
                            isActive
                                ? "bg-card font-bold text-brand-text shadow-sm"
                                : "text-muted hover:text-brand-text"
                        }`}
                    >
                        {lang.label}
                    </button>
                )
            })}
        </div>
    )
}
