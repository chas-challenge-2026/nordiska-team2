export type SavingsGoalsData = {
    id: number;
    name: string;
    targetAmount: number;
    deadline: string | null;
    accountId: number | null;
    currentAmount: number | null;
    progressPercent: number | null;
}