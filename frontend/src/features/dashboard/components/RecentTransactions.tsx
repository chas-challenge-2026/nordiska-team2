import { Link } from "react-router-dom";
import type { Transaction } from "../../../types/transaction";
import ListItem from "../../../components/ui/ListItem";

type RecentTransactionProps = {
    transactions: Transaction[];
    isLoading: boolean;
};

const amountFormatter = new Intl.NumberFormat("sv-SE", {
    style: "currency",
    currency: "SEK"
});

const dateFormatter = new Intl.DateTimeFormat("sv-SE",{
    dateStyle: "medium",
});

export default function RecentTransactions({ transactions, isLoading }: RecentTransactionProps) {
    const latestTransactions = [...transactions]
        .sort(
            (first, second) =>
                Date.parse(second.date) - Date.parse(first.date),
        )
        .slice(0, 5);

    return (
        <section className="overflow-hidden rounded-card
                            border border-border bg-card shadow-sm"
                aria-labelledby="recent-transactions-title">

                <header className="flex items-center justify-between
                                    border-b border-border-light px-5 py-4">
                    <h2 id="recent-transactions-title"
                        className="font-semibold text-medium text-foreground">
                            Senaste händelser
                    </h2>
                    <Link to="/transactions"
                          className="text-small text-brand-text hover:underline mr-2
                                     focus-visible:outline-2 focus-visible:outline-brand">
                        Visa alla
                    </Link>
                </header>

                {isLoading ? (
                    <p className="p-3 text-small text-muted">Laddar händelser...</p>
                ) : latestTransactions.length === 0 ? ( 
                    <p className="p-3 text-small text-muted">Inga händelser än.</p>
                ) : (

                <ul className="divide-y divide-border-light px-5">
                    {latestTransactions.map((transaction) => (
                        <ListItem
                            key={transaction.id}
                            title={transaction.description}
                            subtitle={transaction.account}
                            right={
                                <div className="flex items-center justify-between 
                                                gap-1 sm:flex-col sm:items-end ">
                                    <p className={transaction.amount >= 0
                                                ? "font-semibold text-success text-medium whitespace-nowrap"
                                                : "font-semibold text-foreground text-medium"}>
                                                    {transaction.amount > 0 
                                                        ? "+"
                                                        : ""}
                                                    {amountFormatter.format(transaction.amount)}
                                    </p> 
                                    <p className="whitespace-nowrap text-xsmall text-muted">
                                    {dateFormatter.format(new Date(transaction.date))}
                                </p>
                            </div>
                            }
                        />
                    ))}
                
                </ul>
                ) }
                
        </section>
    )
}
