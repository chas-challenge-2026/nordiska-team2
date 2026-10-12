import AccountCard from "../../../components/cards/AccountCard";
import type { Account } from "../../../types/account"

type AccountOverviewProps = {
    accounts: Account[]; 
}

export default function AccountOverview({ accounts }: AccountOverviewProps) {
    if (accounts.length === 0) {
        return (
            <p className="text-samll text-muted"> Du har inga konton än.</p>
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
                                        to={`/transactions?accountId=${account.id}`}
                                    />
                                ))}         
            </div>
    )
}
