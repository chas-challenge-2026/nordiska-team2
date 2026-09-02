import AccountCard from "./AccountCard";
import type { Account } from "../data/accounts";

type AccountOverviewProps = {
    accounts: Account[]; 
}

export default function AccountOverview({ accounts }: AccountOverviewProps) {
    return (
            <div className="grid grid-cols-1 justify-items-stretch 
                            md:grid-cols-2 xl:grid-cols-3 gap-3">
                                {accounts.map((account) => (
                                    <AccountCard
                                        key={account.id}  
                                        name={account.name}
                                        accountNumber={account.accountNumber}
                                        balance={account.balance}
                                        interest={account.interest}
                                        to={account.to}
                                    />
                                ))}         
            </div>
    )
}
