import type { TFunction } from "i18next"
import type { NotificationItem } from "../types/notification"

export function formatNotificationMessage(
    item: NotificationItem,
    t: TFunction,
    currencyFormatter: Intl.NumberFormat,
): string {
    if (item.amount != null && item.accountNumber) {
        const formattedAmount = currencyFormatter.format(item.amount)

        if (item.type === "deposit_confirmation") {
            return t("settings.notifications.messages.deposit_confirmation", {
                amount: formattedAmount,
                account: item.accountNumber,
            })
        }

        if (item.type === "withdrawal_confirmation") {
            return t("settings.notifications.messages.withdrawal_confirmation", {
                amount: formattedAmount,
                account: item.accountNumber,
            })
        }

        if (item.type === "transfer_confirmation" && item.counterpartAccountNumber) {
            return t("settings.notifications.messages.transfer_confirmation", {
                amount: formattedAmount,
                fromAccount: item.accountNumber,
                toAccount: item.counterpartAccountNumber,
            })
        }
    }

    return item.message
}

