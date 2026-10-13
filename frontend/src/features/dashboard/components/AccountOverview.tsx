import AccountCard from "../../../components/cards/AccountCard";
import type { Account } from "../../../types/account"
import { useTranslation } from "react-i18next";

const MAX_SLOTS = 3;

type AccountOverviewProps = {
    accounts: Account[];
    onCreateAccount: () => void;
}

export default function AccountOverview({ accounts, onCreateAccount }: AccountOverviewProps) {
    const { t } = useTranslation();
    const placeholderCount = Math.max(1, MAX_SLOTS - accounts.length); // Alltid en placeholder.

    return (
            <div className="grid grid-cols-1 justify-items-stretch
                            md:grid-cols-2 xl:grid-cols-3 gap-3
                            ">
                                {accounts.map((account) => (
                                    <AccountCard
                                        key={account.id}
                                        name={account.name}
                                        accountNumber={account.accountNumber}
                                        balance={account.balance}
                                        interest={account.interest}
                                        to={`/transactions?accountId=${account.id}`}
                                    />
                                ))}
                                {Array.from({ length: placeholderCount }).map((_, i) => (
                                    <div key={`placeholder-${i}`} onClick={onCreateAccount} className="cursor-pointer">
                                        <AccountCard
                                            name={t("dashboard.createAccountCard.title")}
                                            accountNumber={t("dashboard.createAccountCard.subtitle")}
                                        />
                                    </div>
                                ))}
            </div>
    )
}
