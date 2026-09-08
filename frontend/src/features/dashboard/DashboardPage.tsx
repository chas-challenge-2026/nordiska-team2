import { useState } from "react";
import AccountOverview from "./components/AccountOverview";
import { accounts } from "./data/accounts";
import RecentTransactions from "./components/RecentTransactions";
import { transactions } from "./data/transactions";
import QuickActions from "./components/QuickActions";
import { quickActions } from "./data/quickActions";

import FinancialOverview from "./components/FinancialOverview"
import { financialOverview } from "./data/financialOverview";
import SavingsGoal from "./components/SavingsGoal";
import { savingsGoal } from "./data/savingsGoal";
import FaqOverview from "./components/Faq";
import SearchBar from "./components/Searchbar";
import Footer from "./components/Footer";
import Button from "../../components/ui/Button";
import TestModal from "./components/TestModal";

export default function DashboardPage() {
    const[isModalOpen, setIsModalOpen] = useState(false);

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
                <QuickActions actions={quickActions}/>
                <RecentTransactions transactions={transactions} />
                


                <Footer />
            </section>

            <aside className=" flex flex-col gap-4 min-w-0 w-full 
                                lg:col-span-4 mt-3 lg:mt-0">
                        <SearchBar />
                        <FinancialOverview 
                            accounts={accounts}
                            financial={financialOverview}
                            />
                        <SavingsGoal 
                            accounts={accounts}
                            goal={savingsGoal.goal} />
                        <FaqOverview />

                    <Button 
                        label="Klicka här"
                        onClick={() => setIsModalOpen(true)} 
                        variant="success"
                            />
                    <TestModal
                        isOpen={isModalOpen}
                        onClose={() => setIsModalOpen(false)}
                    />

            </aside>
        </div>

    )
}


