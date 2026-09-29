import { Link } from "react-router-dom";
import Card from "../../../components/cards/Card";
import { useFinancialSummary } from "../../../hooks/useFinancialSummary";
import type { Account } from "../../../types/account"
import BalanceChart from "./BalanceCharts";

type FinancialOverviewProps = {
    accounts: Account[];
}

const currencyFormatter = new Intl.NumberFormat("sv-SE", {
    style: "currency",
    currency: "SEK",
});

export default function FinancialOverview({ 
    accounts }: FinancialOverviewProps) {
        const { data: financial, isLoading: financialLoading, isError: financialError  } = useFinancialSummary();
        const totalBalance = accounts.reduce(
            (total, account) => total + account.balance,
            0,
        );
    return(
            <Card title="Ekonomisk Översikt"
            headerVariant="secondary">
                    <ul className="divide-y divide-border-light
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
                            {financialLoading 
                                ? ( <p className="text-small text-muted">Laddar ...</p>
                            ) : financialError 
                                ? ( <p className="text-small text-muted">Kunde inte hämta</p>
                            ) : ( <p className="text-balance font-bold text-success text-small">
                                {currencyFormatter.format(financial?.income ?? 0)} 
                            </p> )}
                        </li>
                        <li className="pb-2">
                            <p className="text-small text-muted">
                                Utgifter
                            </p>
                            <p className="text-balance font-bold text-brand text-small">
                                −{currencyFormatter.format(financial?.expenses ?? 0)}
                            </p>
                        </li>
                        <li>
                            <BalanceChart accounts={accounts} />
                        </li>
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
                        Se din ekonomiska översikt
                    </Link>
            </footer>
            </Card>
    )
}