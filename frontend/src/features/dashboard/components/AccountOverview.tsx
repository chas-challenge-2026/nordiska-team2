import AccountCard from "../../../components/cards/AccountCard";
import type { Account } from "../../../types/account"
import { useTranslation } from "react-i18next";

type AccountOverviewProps = {
    accounts: Account[]; 
}

export default function AccountOverview({ accounts }: AccountOverviewProps) {
    const { t } = useTranslation();
    if (accounts.length === 0) {
        return (
            <p className="text-small text-muted">{t("dashboard.noAccounts")}</p>
        )
    }
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
                                        to="/transactions"
                                    />
                                ))}         
            </div>
    )
}
