import { useTranslation } from "react-i18next"
import ListItem from "../ui/ListItem"
import { useNotifications } from "../../hooks/useNotifications"
import { useFormatters } from "../../language/useFormatters"
import { formatNotificationMessage } from "../../lib/notificationMessage"
import type { NotificationStatus } from "../../types/notification"

const NOTIFICATIONS_LIMIT = 50

const STATUS_BADGE_CLASSES: Record<NotificationStatus, string> = {
    sent: "bg-success/15 text-success",
    pending: "bg-accent/15 text-brand-text",
    failed: "bg-cancel/15 text-cancel",
}

export default function NotificationsCard() {
    const { t } = useTranslation()
    const { locale, currency } = useFormatters()
    const { data: notifications, isLoading, isError } = useNotifications(NOTIFICATIONS_LIMIT)

    return (
        <section className="overflow-hidden rounded-xl border border-border bg-card">
            <div className="border-b border-border px-6 py-5">
                <h2 className="text-large font-semibold text-foreground">{t("settings.notifications.title")}</h2>
                <p className="text-small text-muted">{t("settings.notifications.hint")}</p>
            </div>

            <div className="px-6 py-4">
                {isLoading && <p className="text-small text-muted">{t("common.loading")}</p>}
                {isError && <p className="text-small">{t("settings.notifications.error")}</p>}
                {notifications?.length === 0 && <p className="text-small text-muted">{t("settings.notifications.empty")}</p>}

                {notifications && notifications.length > 0 && (
                    <div
                        tabIndex={0}
                        aria-label={t("settings.notifications.title")}
                        className="max-h-80 overflow-y-auto pr-2 rounded-md focus:outline-none focus-visible:ring-2 focus-visible:ring-brand-text"
                    >
                        <ul className="divide-y divide-border">
                            {notifications.map((item) => {
                                const timestamp = item.sentAt ?? item.createdAt
                                const badgeClass = STATUS_BADGE_CLASSES[item.status] ?? STATUS_BADGE_CLASSES.pending
                                const localizedMessage = formatNotificationMessage(item, t, currency)

                                return (
                                    <ListItem
                                        key={item.id}
                                        title={t(`settings.notifications.types.${item.type}`, { defaultValue: item.type })}
                                        subtitle={localizedMessage}
                                        right={
                                            <div className="flex flex-col items-end gap-1">
                                                <span className={`rounded-full px-2.5 py-0.5 text-xsmall font-semibold ${badgeClass}`}>
                                                    {t(`settings.notifications.status.${item.status}`, { defaultValue: item.status })}
                                                </span>
                                                <span className="text-small text-muted whitespace-nowrap">
                                                    {new Date(timestamp).toLocaleString(locale, { dateStyle: "medium", timeStyle: "short" })}
                                                </span>
                                            </div>
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
