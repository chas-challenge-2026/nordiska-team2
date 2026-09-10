import { useQuery } from "@tanstack/react-query";
import { apiClient } from "../client";
import type { Account } from "../types/account";

export function useAccounts() {
    return useQuery({
        queryKey: ["accounts"],
        queryFn: async () => {
            const response = await apiClient.get<Account[]>("/Accounts")
            return response.data
        },
    })
}