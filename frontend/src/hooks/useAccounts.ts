import { useQuery } from "@tanstack/react-query";
import { apiClient } from "../client";
import type { Account } from "../types/account";



// HÄMTAR FRÅN BACKEND
// export function useAccounts() {
//     return useQuery({
//         queryKey: ["accounts"],
//         queryFn: async () => {
//             const response = await apiClient.get<Account[]>("/Accounts")
//             return response.data
//         },
//     })
// } 


// TILLFÄLLIGT KOD MED ANNA (id 1)
export function useAccounts() {
    return useQuery({
        queryKey:["accounts"],
        queryFn: async () => {
            const response = await apiClient.get<Account[]>("/Accounts/1")
            return response.data
        },
    })
}