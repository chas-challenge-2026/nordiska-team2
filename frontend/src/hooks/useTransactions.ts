import { useQuery, useQueries } from "@tanstack/react-query";
import { apiClient } from "../client";
import type { Transaction } from "../types/transaction"
import type { Account } from "../types/account";

type LedgerEntryDto = {
    date: string;
    description: string;
    amount: number;
};

function mapEntries(accountId: number, accountLabel: string, entries: LedgerEntryDto[]): Transaction[] {
    return entries.map((entry, index) => ({
        id: `${accountId}-${index}`,
        description: entry.description,
        account: accountLabel,
        date: entry.date,
        amount: entry.amount,
    }));
}


export function useTransactions(accountId: number, accountLabel: string) {
    return useQuery({
        queryKey: ["transactions", accountId],
        queryFn: async () => {
            const response = await apiClient.get<LedgerEntryDto[]>(`/Transactions/${accountId}`)
            return mapEntries(accountId, accountLabel,response.data);
        },
        enabled: Number.isFinite(accountId),
    })
}

//Hämtar transaktioner för alla konton samtidigt och slår ihop den till en lista
export function useRecentTransactions(accounts: Account[] | undefined) {
    const queries = useQueries({
        queries: (accounts ?? []).map((account) => ({
            queryKey: ["transactions", account.id],
            queryFn: async () => {
                const response = await apiClient.get<LedgerEntryDto[]>(`/Transactions/${account.id}`)
                return mapEntries(account.id, account.accountNumber, response.data)
            }
        }))
    });

    return {
        data: queries.flatMap((q) => q.data ?? []),
        isLoading: queries.some((q) => q.isLoading),
        isError: queries.some ((q) => q.isError),
    }
}