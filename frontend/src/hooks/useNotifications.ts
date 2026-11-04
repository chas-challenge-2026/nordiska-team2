import { useQuery } from "@tanstack/react-query"
import { apiClient } from "../client"
import type { NotificationItem } from "../types/notification"

export function useNotifications(limit = 50) {
    return useQuery({
        queryKey: ["notifications", limit],
        queryFn: async () => (await apiClient.get<NotificationItem[]>("/notifications", { params: { limit } })).data,
        // Smart background polling: only re-checks while at least one notification is still "pending"
        // so the UI transitions from "pending" -> "sent" and fires the confirmation toast automatically,
        // then stops polling completely when all notifications are settled.
        refetchInterval: (query) => {
            const hasPending = query.state.data?.some((n) => n.status === "pending")
            return hasPending ? 1000 : false
        },
    })
}
