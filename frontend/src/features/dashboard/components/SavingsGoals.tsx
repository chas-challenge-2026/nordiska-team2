import Card from "../../../components/cards/Card";
import type { SavingsGoalsData } from "../data/savingsGoalsData";

type SavingOverviewProps = {
    goals: SavingsGoalsData[];
};

const currencyFormatter = new Intl.NumberFormat("sv-SE", {
    style: "currency",
    currency: "SEK",
});

export default function SavingsGoal({
    goals,
}: SavingOverviewProps){
    const goal = goals[0]
    if(!goal) return null;

    const progress = goal.progressPercent ?? 0;
    const currentAmount = goal.currentAmount ?? 0;
    const remaining = Math.max(goal.targetAmount - currentAmount, 0)

    return(
        <Card title={`Sparmål - ${goal.name}`} 
                headerVariant="secondary">
            <div className="flex flex-col gap-3">
                <div>
                    <p className="text-small text-muted">
                        {currencyFormatter.format(currentAmount)}
                    </p>
                    </div>
                <div
                    role="progressbar"
                    aria-label={`Sparmål: ${goal.name}`}
                    aria-valuemin={0}
                    aria-valuemax={goal.targetAmount}
                    aria-valuenow={currentAmount}
                    className="h-3 overflow-hidden rounded-full bg-border-light">
                        <div className="h-full rounded-full bg-accent"
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