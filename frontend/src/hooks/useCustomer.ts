import { useQuery } from "@tanstack/react-query";
import { apiClient } from "../client";

type Customer = {
    id: number;
    name: string;
    email: string;
    personalId: string; 
    address: string;
}

export function useCustomer() {
    return useQuery({
        queryKey: ["customer", "me"],
        queryFn: async () => {
            const response = await apiClient.get<Customer>("customers/me");
            return response.data
        },
    });
}