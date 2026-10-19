import { useQuery } from "@tanstack/react-query";
import { apiClient } from "../client";

type FinancialSummary = {
    income: number;
    expenses: number;
    periodStart: string;
    periodEnd: string;
}

export function useFinancialSummary() {
    return useQuery({
        queryKey: ["accounts", "financial-summary"],
        queryFn: async () => {
            const response = await apiClient.get<FinancialSummary>("/accounts/financial-summary");
            return response.data
        }
    })
}