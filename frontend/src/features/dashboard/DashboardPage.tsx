import AccountCard from "./components/AccountCard";
    

    const accounts= [
        {
            id: 1,
            name: "Lönekonto",
            accountNumber: "1234 556 789-01",
            balance: "3 427,00"
        },
        {
            id: 2,
            name: "Sparkonto",
            accountNumber: "6548 185 881-09",
            balance: "130 000,00"
        },
        {
            id: 3,
            name: "ISK",
            accountNumber: "2234 577 123-06",
            balance: "50 100,00"
        },
    ];
    

export default function DashboardPage() {

    return ( 
            <>
                    <header className="mb-6">
                        <h1 className="text-section-title">
                            Välkommen tillbaka, NAMN!
                        </h1>
                        <p className="text-muted">
                            Inloggad via BankID
                        </p>
                    </header>
                        <div className="grid md:grid-cols-2 xl:grid-cols-3 gap-5">
                            {accounts.map((accounts) => (
                                <AccountCard
                                key={accounts.id}  
                                name={accounts.name}
                                accountNumber={accounts.accountNumber}
                                balance={accounts.balance}
                                />
                            ))}         
                        </div>

            </>
    )
}

