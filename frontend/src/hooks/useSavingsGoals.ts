import { apiClient } from "../client";
import type { SavingsGoalsData } from "../features/dashboard/data/savingsGoalsData";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

export function useSavingsGoals() {
    return useQuery({
        queryKey: ["savings-goals"],
        queryFn: async () => {
            const response = await apiClient.get<SavingsGoalsData[]>("/savings-goals");
            return response.data;
        },
    });
}

export function useDeleteSavingsGoals() {
    const queryClient = useQueryClient()
    const deleteMutation = useMutation({
        mutationFn: (goalId: number) => apiClient.delete(`/savings-goals/${goalId}`),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ["savings-goals"] })
        },
    })
    return deleteMutation;
}
