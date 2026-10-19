export type TransactionType = "Deposit" | "Withdrawal" | "Interest" | "Tax" | "TransferIn" | "TransferOut";

// One entry from GET /Transactions/{accountId}. Shared here, so the hook,
// the transaction page and the chart all use the same shape.
export type LedgerEntryDto = {
    date: string;
    description: string | null;              // null = the customer wrote nothing
    amount: number;
    type: TransactionType;
    counterpartAccountNumber: string | null; // transfers only
};

export type Transaction = {
    id: string;
    description: string | null;
    type: TransactionType;
    counterpartAccountNumber: string | null;
    account: string;
    date: string;
    amount: number;
    icon?: string;
};