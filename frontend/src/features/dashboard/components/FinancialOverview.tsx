import { Link } from "react-router-dom";
import Card from "../../../components/cards/Card";
import type { FinancialOverviewData } from "../data/financialOverview";
import type { Account } from "../data/accounts";

type FinancialOverviewProps = {
    accounts: Account[];
    financials: FinancialOverviewData;
}

const currencyFormatter = new Intl.NumberFormat("sv-SE", {
    style: "currency",
    currency: "SEK",
});

export default function FinancialOverview({ 
    accounts,
    financials }: FinancialOverviewProps){
        const totalBalance = accounts.reduce(
            (total, account) => total + account.balance,
            0,
        );
    return(
            <Card title="Ekonomisk Översikt"
            headerVariant="secondary">
                    <ul className="divide-y devide-y-reverse divide-border-light
                                    flex flex-col gap-3" >
                        <li className="mt-2 pb-2">
                            <p className="text-small text-muted">
                                Totalt saldo
                            </p>
                            <p className="text-balance font-bold text-brand">
                                {currencyFormatter.format(totalBalance)}
                            </p>
                        </li>
                        <li className="pb-2">
                            <p className="text-small text-muted">
                                Inkomster
                            </p>
                            <p className="text-balance font-bold text-success text-small">
                                {currencyFormatter.format(financials.income)}
                            </p>
                        </li>
                        <li className="pb-2">
                            <p className="text-small text-muted">
                                Utgifter
                            </p>
                            <p className="text-balance font-bold text-brand text-small">
                                −{currencyFormatter.format(financials.expenses)}
                            </p>
                        </li>
                </ul>
                <footer className="border-t border-border-light text-center">
                    <Link
                        to="/transactions"
                        className="flex w-full items-center 
                                justify-center 
                                px3 py-2 text-small text-brand
                                transition hover:bg-background
                                focus-visible:outline-2
                                focus-visible:outline-offset-2
                                focus-visible:outline-brand
                                sm:px-4"
                    >
                        Se din ekonomiska översikt
                    </Link>
            </footer>
            </Card>
    )
}