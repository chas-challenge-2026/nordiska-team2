import AccountCard from "./components/AccountCard";
import LinkButton from "../../components/ui/LinkButton";
import transactionIcon from "../../../public/svg/transitions-dark.svg"
import taxReportIcon from "../../../public/svg/taxes-dark.svg"
import blockIcon from "../../../public/svg/block-dark.svg"
import bankIdIcon from "../../../public/svg/bankid-dark.svg"
import cardIcon from "../../../public/svg/credit-dark.svg"
    
    const accounts= [
        {
            id: 1,
            name: "Lönekonto",
            accountNumber: "1234 556 789-01",
            balance: "3 427,00",
            to: "/transactions"
        },
        {
            id: 2,
            name: "Sparkonto",
            accountNumber: "6548 185 881-09",
            balance: "130 000,00",
            interest: "2,13",
            to: "/transactions"
        },
        {
            id: 3,
            name: "ISK",
            accountNumber: "2234 577 123-06",
            balance: "50 100,00",
            interest: "2,43",
            to: "/transactions"
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

                <div className="grid justify-items-center md:grid-cols-2 
                                xl:grid-cols-3 gap-5 
                                border-b pb-10">
                    {accounts.map((accounts) => (
                        <AccountCard
                        key={accounts.id}  
                        name={accounts.name}
                        accountNumber={accounts.accountNumber}
                        balance={accounts.balance}
                        interest={accounts.interest}
                        to={accounts.to}
                        />
                    ))}         
                </div>

                <div className="flex flex-wrap justify-center 
                                gap-5 mt-10">
                    <LinkButton 
                    to="/transactions"
                    size="small"
                    icon={
                        <img src={transactionIcon}
                        alt=""
                        aria-hidden="true"
                        className="h-10 w-10"
                        />
                    }>
                        Insättning/Uttag
                    </LinkButton>

                    <LinkButton 
                        to="/taxreport"
                        size="small"  
                        icon={
                            <img src={taxReportIcon}
                            alt=""
                            aria-hidden="true"
                            className="h-10 w-10"
                            />
                    }>
                            Betala räkningar
                    </LinkButton>

                    <LinkButton 
                        to="/taxreport"
                        size="small"
                        icon={
                            <img src={blockIcon}
                            alt=""
                            aria-hidden="true"
                            className="h-10 w-10"
                            />
                    }>
                            Spärra kort
                    </LinkButton>

                    <LinkButton 
                        to="/taxreport"
                        size="small"
                        icon={
                            <img src={bankIdIcon}
                            alt=""
                            aria-hidden="true"
                            className="h-10 w-10"
                            />
                    }>
                            BankID
                    </LinkButton>
                    <LinkButton 
                        to="/taxreport"
                        size="small"
                        icon={
                            <img src={cardIcon}
                            alt=""
                            aria-hidden="true"
                            className="h-10 w-10"
                            />
                    }>
                            Ansök om lån
                    </LinkButton>
                </div>
            </>
    )
}

