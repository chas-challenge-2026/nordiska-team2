import { useEffect, useRef } from "react"
import { useTranslation } from "react-i18next"
import { useNotifications } from "../../hooks/useNotifications"
import { useAlert } from "../../hooks/useAlert"
import { useFormatters } from "../../language/useFormatters"
import { formatNotificationMessage } from "../../lib/notificationMessage"
import type { NotificationStatus } from "../../types/notification"

export default function NotificationToastListener() {
    const { data: notifications } = useNotifications(50)
    const { showAlert } = useAlert()
    const { t } = useTranslation()
    const { currency } = useFormatters()

    // Tracks notification statuses we have already seen in this session
    // so historical notifications don't spam toasts on initial login.
    const seenStatusRef = useRef<Map<number, NotificationStatus> | null>(null)

    useEffect(() => {
        if (!notifications) return

        // Initial load: record existing notifications without firing toasts for old history
        if (seenStatusRef.current === null) {
            const initialMap = new Map<number, NotificationStatus>()
            for (const item of notifications) {
                initialMap.set(item.id, item.status)
            }
            seenStatusRef.current = initialMap
            return
        }

        const seen = seenStatusRef.current

        for (const item of notifications) {
            const previousStatus = seen.get(item.id)

            // Fire a single localized confirmation toast when the background worker finishes ("sent")
            if (item.status === "sent" && previousStatus !== "sent") {
                showAlert({
                    type: "success",
                    title: t(`settings.notifications.types.${item.type}`, { defaultValue: item.type }),
                    message: formatNotificationMessage(item, t, currency),
                    duration: 6000,
                })
            } else if (item.status === "failed" && previousStatus !== "failed") {
                showAlert({
                    type: "error",
                    title: t(`settings.notifications.types.${item.type}`, { defaultValue: item.type }),
                    message: t("settings.notifications.status.failed"),
                    duration: 6000,
                })
            }

            seen.set(item.id, item.status)
        }
    }, [notifications, showAlert, t, currency])

    return null
}
