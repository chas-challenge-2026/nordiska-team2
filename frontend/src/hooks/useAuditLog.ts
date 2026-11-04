import { useQuery } from "@tanstack/react-query"
import { apiClient } from "../client"

export type AuditEntry = {
    action: string
    refId: string | null
    timestamp: string
    ipAddress: string | null
}

export function useAuditLog(limit = 50) {
    return useQuery({
        queryKey: ["audit", limit],
        queryFn: async () => (await apiClient.get<AuditEntry[]>("/audit", { params: { limit } })).data,
    })
}