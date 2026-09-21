import { Link } from "react-router-dom";
import type { Transaction } from "../../../types/transaction";
import ListItem from "../../../components/ui/ListItem";

type RecentTransactionProps = {
    transactions: Transaction[];
};

const amountFormatter = new Intl.NumberFormat("sv-SE", {
    style: "currency",
    currency: "SEK"
});

const dateFormatter = new Intl.DateTimeFormat("sv-SE",{
    dateStyle: "medium",
});

export default function RecentTransactions({ transactions }: RecentTransactionProps) {
    const latestTransactions = [...transactions]
        .sort(
            (first, second) =>
                Date.parse(second.date) - Date.parse(first.date),
        )
        .slice(0, 5);

    return (
        <section className="overflow-hidden rounded-default
                            border border-border bg-card shadow-sm"
                aria-labelledby="recent-transactions-title">
                
                <header className="border-b border-border-light 
                                    px-3 py-2 sm:px-4">
                    <h2 id="recent-transactions-title"
                        className="font-semibold text-medium text-brand">
                            Senaste händelser
                    </h2>
                </header>

                <ul className="divide-y divide-border-light">
                    {latestTransactions.map((transaction) => (
                        <ListItem
                            key={transaction.id}
                            title={transaction.description}
                            subtitle={transaction.account}
                            right={
                                <div className="flex items-center justify-between 
                                                gap-3 sm:flex-col sm:items-end">
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
                
                <footer className="border-t border-border-light text-center">
                <Link
                    to="/transactions"
                    className="flex w-full items-center 
                               justify-center 
                               px-3 py-2 text-small text-brand
                               transition hover:bg-background
                               focus-visible:outline-2
                               focus-visible:outline-offset-2
                               focus-visible:outline-brand
                               sm:px-4"
                >
                    Visa alla händelser
                </Link>
            </footer>

        </section>
    )
}
