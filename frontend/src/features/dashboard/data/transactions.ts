export type Transactions = {
    id: string;
    description: string;
    account: string;
    date: string;
    amount: string
    icon?: string;
}

export type Transaction = {
    id: string;
    description: string;
    account: string;
    date: string;
    amount: number;
};

export const transactions: Transaction[] = [
    {
        id: "transaction-1",
        description: "Lön",
        account: "Lönekonto",
        date: "2026-09-01",
        amount: 28450,
    },
    {
        id: "transaction-2",
        description: "ICA Maxi",
        account: "Lönekonto",
        date: "2026-08-31",
        amount: -842.5,
    },
    {
        id: "transaction-3",
        description: "Överföring till sparande",
        account: "Sparkonto",
        date: "2026-08-30",
        amount: -2000,
    },
    {
        id: "transaction-4",
        description: "Spotify",
        account: "Lönekonto",
        date: "2026-08-29",
        amount: -119,
    },
    {
        id: "transaction-5",
        description: "Ränteinsättning",
        account: "Sparkonto",
        date: "2026-08-28",
        amount: 92.5,
    },
];