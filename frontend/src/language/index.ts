import i18n from "i18next"
import { initReactI18next } from "react-i18next"
import sv from "./locales/sv.json"
import en from "./locales/en.json"

i18n.use(initReactI18next).init({
    resources: {
        sv: { translation: sv },
        en: { translation: en },
    },
    lng: localStorage.getItem("language") ?? "sv",
    fallbackLng: "sv",
    interpolation: { escapeValue: false },
})

// Sätt <html lang> direkt vid start (skärmläsare uttalar då texten rätt).
document.documentElement.lang = i18n.language

// Varje gång språket byts: spara valet och uppdatera <html lang>.
// Därför räcker det att anropa i18n.changeLanguage() i resten av appen.
i18n.on("languageChanged", (lng) => {
    localStorage.setItem("language", lng)
    document.documentElement.lang = lng
})

export default i18n
