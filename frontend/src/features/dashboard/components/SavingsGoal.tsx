import Card from "../../../components/cards/Card";
import type { Account } from "../data/accounts";

type SavingOverviewProps = {
    accounts: Account[];
    goal: number;
};

const currencyFormatter = new Intl.NumberFormat("sv-SE", {
    style: "currency",
    currency: "SEK",
});

export default function SavingsGoal({
    accounts,
    goal,
}: SavingOverviewProps){
    const savingsBalance = accounts
    .filter((account) => account.type === "savings")
    .reduce((total, account) => total + account.balance, 0)

    const progress =
    goal > 0
        ? Math.min((savingsBalance / goal) * 100, 100)
        : 0;

    const remaining = Math.max(goal - savingsBalance, 0)

    return(
        <Card title="Sparmål - Japan"
                headerVariant="secondary">
            <div className="flex flex-col gap-3">
                <div>
                    <p className="text-small text-muted">
                        {currencyFormatter.format(savingsBalance)}
                    </p>
                </div>
                <div
                    role="progressbar"
                    aria-label="Sparmål till Japan"
                    aria-valuemin={0}
                    aria-valuemax={goal}
                    aria-valuenow={savingsBalance}
                    className="h-3 overflow-hidden rounded-full bg-border-light">
                        <div className="h-full rounded-full bg-success"
                            style={{ width: `${progress}%` }} />
                </div>
                <p className="text-small text-muted">
                    {remaining > 0
                        ? `${currencyFormatter.format(remaining)} kvar till målet`
                        : "Målet är uppnått!"}
                </p>
            </div>
        </Card>
    )
}