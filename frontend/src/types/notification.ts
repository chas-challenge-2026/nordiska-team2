export type NotificationStatus = "pending" | "sent" | "failed"

export type NotificationItem = {
    id: number
    type: string
    refId: string
    message: string
    status: NotificationStatus
    createdAt: string
    sentAt: string | null
    amount?: number | null
    accountNumber?: string | null
    counterpartAccountNumber?: string | null
}
