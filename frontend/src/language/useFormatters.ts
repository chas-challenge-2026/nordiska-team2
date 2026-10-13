import { useTranslation } from "react-i18next"

// Ger rätt format för belopp och datum utifrån valt språk.
// sv → "1 234,50 kr" och "12 okt. 2026", en → "SEK 1,234.50" och "12 Oct 2026".
export function useFormatters() {
    const { i18n } = useTranslation()
    const locale = i18n.language === "en" ? "en-GB" : "sv-SE"

    const currency = new Intl.NumberFormat(locale, {
        style: "currency",
        currency: "SEK",
    })

    return { locale, currency }
}
