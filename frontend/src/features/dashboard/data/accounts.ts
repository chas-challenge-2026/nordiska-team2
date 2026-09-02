
export type Account = {
    id: number;
    name: string;
    accountNumber: string;
    balance: string;
    interest?: string;
    to: string;
}

export const accounts: Account[] = [
        {
            id: 1,
            name: "Lönekonto",
            accountNumber: "1234 556 789-01",
            balance: "3 427,00",
            to: "/transactions"
        },
        {
            id: 2,
            name: "Sparkonto",
            accountNumber: "6548 185 881-09",
            balance: "130 000,00",
            interest: "2,13",
            to: "/transactions"
        },
        {
            id: 3,
            name: "ISK",
            accountNumber: "2234 577 123-06",
            balance: "50 100,00",
            interest: "2,43",
            to: "/transactions"
        },
];