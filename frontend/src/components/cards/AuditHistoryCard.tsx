import { useTranslation } from "react-i18next"
import ListItem from "../ui/ListItem"
import { useAuditLog } from "../../hooks/useAuditLog"

// Login-related rows also show the IP address, so the customer can spot logins that weren't theirs.
const LOGIN_ACTIONS = new Set(["login", "login_failed", "bankid_login", "logout"])

// How many events to fetch. About 5 are visible at once; the rest are reached by scrolling.
// The API returns at most 100.
const HISTORY_LIMIT = 50

export default function AuditHistoryCard() {
    const { t, i18n } = useTranslation()
    const { data: entries, isLoading, isError } = useAuditLog(HISTORY_LIMIT)
    const locale = i18n.language.startsWith("sv") ? "sv-SE" : "en-GB"

    return (
        <section className="overflow-hidden rounded-xl border border-border bg-card">
            <div className="border-b border-border px-6 py-5">
                <h2 className="text-large font-semibold text-foreground">{t("settings.history.title")}</h2>
                <p className="text-small text-muted">{t("settings.history.hint")}</p>
            </div>

            <div className="px-6 py-4">
                {isLoading && <p className="text-small text-muted">{t("common.loading")}</p>}
                {isError && <p className="text-small">{t("settings.history.error")}</p>}
                {entries?.length === 0 && <p className="text-small text-muted">{t("settings.history.empty")}</p>}

                {entries && entries.length > 0 && (
                    <div
                        tabIndex={0}
                        aria-label={t("settings.history.title")}
                        className="max-h-80 overflow-y-auto pr-2 rounded-md focus:outline-none focus-visible:ring-2 focus-visible:ring-brand-text"
                    >
                        <ul className="divide-y divide-border">
                            {entries.map((entry, i) => {
                                const details = [entry.refId, LOGIN_ACTIONS.has(entry.action) ? entry.ipAddress : null]
                                    .filter(Boolean)
                                    .join(" · ")
                                return (
                                    <ListItem
                                        key={`${entry.timestamp}-${i}`}
                                        title={t(`settings.history.actions.${entry.action}`, { defaultValue: entry.action })}
                                        subtitle={details}
                                        right={
                                            <span className="text-small text-muted">
                                                {new Date(entry.timestamp).toLocaleString(locale, { dateStyle: "medium", timeStyle: "short" })}
                                            </span>
                                        }
                                    />
                                )
                            })}
                        </ul>
                    </div>
                )}
            </div>
        </section>
    )
}