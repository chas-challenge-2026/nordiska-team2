import AccountOverview from "./components/AccountOverview";
import { accounts } from "./data/accounts";
import RecentTransactions from "./components/RecentTransactions";
import { transactions } from "./data/transactions";
import QuickActions from "./components/QuickActions";
import { quickActions } from "./data/quickActions";


export default function DashboardPage() {

    return ( 
            <div className="flex flex-1 flex-col 
                            min-h-0 gap-3 sm:gap-4">
                <header>
                    <h1 className="text-xl sm:text-title">
                        Välkommen tillbaka, NAMN!
                    </h1>
                    <p className="text-muted text-small">
                        Inloggad via BankID
                    </p>
                </header>

                <AccountOverview accounts={accounts}/>
                <RecentTransactions transactions={transactions} />
                <div className="mt-auto">
                    <QuickActions actions={quickActions}/>
                </div>
            </div>
    )
}

