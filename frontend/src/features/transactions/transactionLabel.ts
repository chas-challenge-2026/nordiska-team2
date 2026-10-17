import type { TFunction } from "i18next";
import type { TransactionType } from "../../types/transaction";

// Anything with these three fields can be labelled: Transaction, LedgerEntryDto, or a page's own type.
type Labelable = {
    description: string | null;
    type: TransactionType;
    counterpartAccountNumber: string | null;
};

// The customer's own text is shown as-is, in every language. 
// Otherwise the label comes from the type, in the reader's language. 
// Call it when rendering and not in a hook,
// React Query caches hook results, and a label made there would stay in the old language after a switch.
export function transactionLabel(tx: Labelable, t: TFunction): string {
    return tx.description ?? t(`transactionType.${tx.type}`, { account: tx.counterpartAccountNumber ?? "" });
}