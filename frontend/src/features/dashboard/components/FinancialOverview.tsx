import { Link } from "react-router-dom";
import Card from "../../../components/cards/Card";
// import { useFinancialSummary } from "../../../hooks/useFinancialSummary"; {/* INKOMSTER OCH UTGIFTER */}
import type { Account } from "../../../types/account"
import BalanceChart from "./BalanceCharts";
import { useTranslation } from "react-i18next";
import { useFormatters } from "../../../language/useFormatters";

type FinancialOverviewProps = {
    accounts: Account[];
}

export default function FinancialOverview({ 
    accounts }: FinancialOverviewProps) {
        const { t } = useTranslation();
        const { currency: currencyFormatter } = useFormatters();
        // const { data: financial, isLoading: financialLoading, isError: financialError  } = useFinancialSummary(); /* INKOMSTER OCH UTGIFTER */
        const totalBalance = accounts.reduce(
            (total, account) => total + account.balance,
            0,
        );
    return(
            <Card title={t("dashboard.financialOverview")}
            headerVariant="secondary">
                    <ul className="divide-y divide-border-light
                                    flex flex-col gap-3" >
                        <li className="mt-2 pb-2">
                            <p className="text-small text-muted">
                                {t("dashboard.totalBalance")}
                            </p>
                            <p className="text-title font-bold text-brand-text">
                                {currencyFormatter.format(totalBalance)}
                            </p>
                        </li>


            {/* ========== INKOMSTER OCH UTGIFTER ========== */}

                        {/* <li className="pb-2">
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
                        </li> */}


                        <li className="mb-3">
                            <BalanceChart accounts={accounts} />
                        </li>
                </ul>
                <footer className="border-t border-border-light text-center">
                    <Link
                        to="/transactions"
                        className="flex w-full items-center 
                                justify-center 
                                px-3 py-2 text-small text-brand-text
                                transition hover:bg-background
                                focus-visible:outline-2
                                focus-visible:outline-offset-2
                                focus-visible:outline-brand
                                sm:px-4"
                    >
                        {t("dashboard.seeFinancialOverview")}
                    </Link>
            </footer>
            </Card>
    )
}