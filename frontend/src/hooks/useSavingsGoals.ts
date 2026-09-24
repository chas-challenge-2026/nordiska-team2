import { useQuery } from "@tanstack/react-query";
import { apiClient } from "../client";
import type { SavingsGoalData } from "../features/dashboard/data/savingsGoalsData";

export function useSavingsGoals() {
    return useQuery({
        queryKey: ["savings-goals"],
        queryFn: async () => {
            const response = await apiClient.get<SavingsGoalData[]>("/savings-goals");
            return response.data;
        },
    });
}
