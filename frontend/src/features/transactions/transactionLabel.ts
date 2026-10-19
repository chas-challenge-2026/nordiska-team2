import type { TFunction } from "i18next";
import type { TransactionType } from "../../types/transaction";

// Anything with these three fields can be labelled: Transaction, LedgerEntryDto, or a page's own type.
type Labelable = {
    description: string | null;
    type: TransactionType;
    counterpartAccountNumber: string | null;
};

// What kind of transaction it is, in the reader's language
// ("Insättning", "Transfer to NKM-10002").
export function transactionTypeLabel(
    tx: Pick<Labelable, "type" | "counterpartAccountNumber">,
    t: TFunction,
): string {
    return t(`transactionType.${tx.type}`, { account: tx.counterpartAccountNumber ?? "" });
}

// The customer's own text as-is; otherwise what kind of transaction it is.
// Call when rendering, not in a hook (React Query caches hook results).
export function transactionLabel(tx: Labelable, t: TFunction): string {
    return tx.description ?? transactionTypeLabel(tx, t);
}