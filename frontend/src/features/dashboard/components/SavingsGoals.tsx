import { useState } from "react";
import { Lottie } from "lottie-react";
import type { Account } from "../../../types/account";
import Card from "../../../components/cards/Card";
import SelectOptions from "../../../components/ui/Select";
import type { OptionType } from "../../../components/ui/Select";
import type { SavingsGoalsData } from "../data/savingsGoalsData";

type SavingOverviewProps = {
    goals: SavingsGoalsData[];
    isLoading: boolean;
    accounts: Account[];
};

const currencyFormatter = new Intl.NumberFormat("sv-SE", {
    style: "currency",
    currency: "SEK",
});

export default function SavingsGoal({
    goals,
    isLoading,
    accounts,
}: SavingOverviewProps){
    const [explicitGoalOption, setSelectedGoalOption] = useState<OptionType | null>(null)
    const goalOptions: OptionType[] = goals.map((goal) => {
        const goalAccount = accounts.find((a) => a.id === goal.accountId);
        return{
        value: String(goal.id),
        label: `${goalAccount?.accountNumber ?? ""} ${goal.name}`,
    }})

const isExplicitGoalValid = explicitGoalOption && goals.some((g) => String(g.id) === explicitGoalOption.value);
const selectedGoalOption = isExplicitGoalValid 
    ? explicitGoalOption 
    : (goals[0] 
        ? { value: String(goals[0].id), label: goals[0].name } 
        : null);
        
    if(isLoading) {
        return(
            <Card title="Sparmål" headerVariant="secondary">
                <p className="text-small text-muted">
                    Laddar sparmål.
                </p>
            </Card>
        )
    }
    
    const goal = goals.find((g) => String(g.id) === selectedGoalOption?.value);
    if(!goal) return null;

    const account = accounts.find((a) => a.id === goal.accountId);
    const progress = goal.progressPercent ?? 0;
    const currentAmount = goal.currentAmount ?? 0;
    const remaining = Math.max(goal.targetAmount - currentAmount, 0)

    return(
        <Card
            headerContent={
                <>
                    {goals.length > 1 ? (
                        <SelectOptions
                            value={selectedGoalOption}
                            onChange= {setSelectedGoalOption}
                            options={goalOptions}
                            className="-ml-1 w-full border-none text-medium font-semibold" />
                    ) : ( 
                        <h2 className="min-h-6 text-medium font-semibold">{goal.name}</h2>
                    )}
                    <p className="text-small opacity-85">Sparmål: <span className="font-semibold">{account?.accountNumber}</span></p> 
                </>
            }
            headerVariant="secondary">
            <div className="flex flex-col gap-3">
                    <p className="text-small text-muted">
                        {currencyFormatter.format(currentAmount)} / {currencyFormatter.format(goal.targetAmount)}
                    </p>
                <div className="flex items-center gap-2">
                    <div
                        role="progressbar"
                        aria-label={goal.name}
                        aria-valuemin={0}
                        aria-valuemax={goal.targetAmount}
                        aria-valuenow={currentAmount}
                        className="h-3 flex-1 overflow-hidden rounded-full bg-border-light">
                            <div className={`h-full rounded-full bg-accent ${progress >= 100 ? "bg-success" : "bg-accent"}`}
                                style={{ width: `${progress}%` }} />
                    </div>
                    <div className="size-7 shrink-0 overflow-hidden">
                        <Lottie
                            src={progress >= 100
                                ? "https://fonts.gstatic.com/s/e/notoemoji/latest/1f31f/lottie.json"
                                : "https://fonts.gstatic.com/s/e/notoemoji/latest/1f427/lottie.json"}
                            loop
                            autoplay
                            className="size-full"
                        />
                    </div>
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