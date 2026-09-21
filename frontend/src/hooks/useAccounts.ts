import { useQuery } from "@tanstack/react-query";
import { apiClient } from "../client";
import type { Account } from "../types/account";

type AccountDto = {
    id: number;
    accountNumber: string;
    accountType: string;
    interestRate: number; 
    balance: number;
}

function mapAccountDto(dto: AccountDto): Account {
    return {
        id: dto.id,
        accountNumber: dto.accountNumber,
        balance: dto.balance,
        interest: Math.round(dto.interestRate * 10000) / 100, // 0.035 -> 3.5 (%)
        type: dto.accountType.toLowerCase() as Account["type"],
        name: dto.accountType === "Savings" ? "Privatkonto" : dto.accountType,
    };
}

export function useAccounts() {
    return useQuery({
        queryKey: ["accounts"],
        queryFn: async () => {
            const response = await apiClient.get<AccountDto[]>("/Accounts")
            return response.data.map(mapAccountDto)
        },
    })
} 