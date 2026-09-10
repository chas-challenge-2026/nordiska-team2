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
    to?: string;
    type: AccountType;
}