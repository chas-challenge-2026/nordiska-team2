import { useQuery } from "@tanstack/react-query";
import { apiClient } from "../client";

type InterestSummary = {
    year: number;
    interestRatePct: number;
    earnedToDate: number;
    projectedAtYearEnd: number;
    interestPaidTotal: number;
    paymentDate: string; // "2026-12-31"
}

// Nyckeln börjar med "accounts", så insättning/uttag (som invaliderar ["accounts"]) hämtar om räntan också
export function useInterest(accountId: number | null) {
    return useQuery({
        queryKey: ["accounts", accountId, "interest"],
        queryFn: async () => {
            const response = await apiClient.get<InterestSummary>(`/accounts/${accountId}/interest`);
            return response.data
        },
        enabled: accountId !== null,
    })
}
