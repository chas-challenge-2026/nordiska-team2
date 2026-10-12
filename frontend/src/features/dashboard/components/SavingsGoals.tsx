import { useState } from "react";
import Card from "../../../components/cards/Card";
import SelectOptions from "../../../components/ui/Select";
import type { OptionType } from "../../../components/ui/Select";
import type { SavingsGoalsData } from "../data/savingsGoalsData";
import { useTranslation } from "react-i18next";
import { useFormatters } from "../../../language/useFormatters";

type SavingOverviewProps = {
    goals: SavingsGoalsData[];
    isLoading: boolean;
};

export default function SavingsGoal({
    goals,
    isLoading
}: SavingOverviewProps){
    const [explicitGoalOption, setSelectedGoalOption] = useState<OptionType | null>(null)
    const { t } = useTranslation();
    const { currency: currencyFormatter } = useFormatters();
    const goalOptions: OptionType[] = goals.map((goal) => ({
        value: String(goal.id),
        label: goal.name,
    }))

const isExplicitGoalValid = explicitGoalOption && goals.some((g) => String(g.id) === explicitGoalOption.value);
const selectedGoalOption = isExplicitGoalValid 
    ? explicitGoalOption 
    : (goals[0] 
        ? { value: String(goals[0].id), label: goals[0].name } 
        : null);
        
    if(isLoading) {
        return(
            <Card title={t("common.savingsGoal")} headerVariant="secondary">
                <p className="text-small text-muted">
                    {t("dashboard.loadingSavingsGoals")}
                </p>
            </Card>
        )
    }
    
    const goal = goals.find((g) => String(g.id) === selectedGoalOption?.value);
    if(!goal) return null;

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
                    <p className="text-small opacity-85">{t("common.savingsGoal")}</p>
                </>
            }
            headerVariant="secondary">
            <div className="flex flex-col gap-3">
                    <p className="text-small text-muted">
                        {currencyFormatter.format(currentAmount)}
                    </p>
                <div
                    role="progressbar"
                    aria-label={goal.name}
                    aria-valuemin={0}
                    aria-valuemax={goal.targetAmount}
                    aria-valuenow={currentAmount}
                    className="h-3 overflow-hidden rounded-full bg-border-light">
                        <div className={`h-full rounded-full bg-accent ${progress >= 100 ? "bg-success" : "bg-accent"}`}
                            style={{ width: `${progress}%` }} />
                </div>
                <p className="text-small text-muted">
                    {remaining > 0
                        ? t("dashboard.remainingToGoal", { amount: currencyFormatter.format(remaining) })
                        : t("dashboard.goalReached")}
                </p>
                
            </div>
        </Card>
    )
}