export type AccountType = 
    | "checking" 
    | "savings" 
    | "investment";

export type Account = {
    id: number;
    name: string;
    accountNumber: string;
    balance: number;
    interest?: number;
    to: string;
    type: AccountType;
}

export const accounts: Account[] = [
        {
            id: 1,
            name: "Lönekonto",
            accountNumber: "1234 556 789-01",
            balance: 3427,
            type: "checking",
            to: "/transactions"
        },
        {
            id: 2,
            name: "Sparkonto",
            accountNumber: "6548 185 881-09",
            balance: 18000,
            interest: 2.13,
            type: "savings",
            to: "/transactions"
        },
        {
            id: 3,
            name: "ISK",
            accountNumber: "2234 577 123-06",
            balance: 50100,
            interest: 2.43,
            type: "investment",
            to: "/transactions"
        },
];