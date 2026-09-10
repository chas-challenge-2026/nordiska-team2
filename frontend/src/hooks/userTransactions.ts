import { useQuery } from "@tanstack/react-query";
import { apiClient } from "../client";
import type { Transaction } from "../types/transaction"

export function useTransactions(accountId: number) {
    return useQuery({
        queryKey: ["transactions", accountId],
        queryFn: async () => {
            const response = await apiClient.get<Transaction[]>(`/Transactions/${accountId}`)

            return response.data;
        },
        enabled: Number.isFinite(accountId),
    })
}