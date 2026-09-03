import AccountOverview from "./components/AccountOverview";
import { accounts } from "./data/accounts";
import RecentTransactions from "./components/RecentTransactions";
import { transactions } from "./data/transactions";
import QuickActions from "./components/QuickActions";
import { quickActions } from "./data/quickActions";

import FinancialOverview from "./components/FinancialOverview"
import { financialOverviews } from "./data/financialOverview";
import SavingsGoal from "./components/SavingsGoal";
import { savingsGoal } from "./data/savingsGoal";
import FaqOverview from "./components/Faq";
import SearchBar from "./components/searchbar";
import Footer from "./components/Footer";


export default function DashboardPage() {

    return ( 
        <div className="grid min-h-full grid-cols-1 lg:grid-cols-[repeat(14,minmax(0,1fr))]">
            <section className="flex flex-1 flex-col 
                            min-h-0 gap-3 sm:gap-4 sm:pr-6 lg:col-span-10">
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
                <QuickActions actions={quickActions}/>

                <Footer />
            </section>

            <aside className=" flex flex-col gap-4 min-w-0 w-full 
                                lg:col-span-4 mt-3 lg:mt-0">
                        <SearchBar />
                        <FinancialOverview 
                            accounts={accounts}
                            financials={financialOverviews}
                            />
                        <SavingsGoal 
                            accounts={accounts}
                            goal={savingsGoal.goal} />
                        <FaqOverview />
            </aside>
        </div>

    )
}


