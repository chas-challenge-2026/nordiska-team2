import { useState } from "react";
import Button from "../../../../../components/ui/Button";
import Modal from "../../../../../components/ui/Modal";
import SelectOptions from "../../../../../components/ui/Select";
import type { OptionType } from "../../../../../components/ui/Select";
import { useSavingsGoals, useDeleteSavingsGoals } from "../../../../../hooks/useSavingsGoals";
import { useTranslation } from "react-i18next";

type DeleteSavingsGoalModalProps = {
    isOpen: boolean;
    onClose: () => void;
};

export default function DeleteSavingsGoalMoal({ isOpen, onClose }: DeleteSavingsGoalModalProps) {
    const { data: goals } = useSavingsGoals(); 
    const deleteMutation = useDeleteSavingsGoals();
    const { t } = useTranslation();

    const goalOptions: OptionType[] = goals?.map((goal) => ({
        value: String(goal.id),
        label: goal.name,
    })) ?? [];

    const [selectedGoal, setSelectedGoal] = useState<OptionType | null>(null)
    const [hasAttemptedSubmit, setHasAttemptedSubmit] = useState(false);       

    function resetAndClose() {
        setSelectedGoal(null);
        setHasAttemptedSubmit(false);
        onClose();
    }

    function handleDelete() {
        setHasAttemptedSubmit(true);
        if (!selectedGoal) return;
        deleteMutation.mutate(Number(selectedGoal.value), {
            onSuccess: resetAndClose,
        });
    }

    return (
        <Modal title={t("deleteGoalModal.title")} 
                isOpen={isOpen}
                onClose={resetAndClose}>

            <div className="flex flex-col gap-1 
                            border border-border rounded-default 
                            w-full h-full p-3"> 
                <p className="mt-2">{t("deleteGoalModal.question")}</p>
                    <SelectOptions
                        value={selectedGoal}
                        onChange={setSelectedGoal}
                        options={goalOptions}
                        placeholder={t("deleteGoalModal.selectGoal")}
                    />
                    {goalOptions.length === 0 && (
                        <p className="text-xsmall text-muted">{t("deleteGoalModal.noGoals")}</p>
                    )}
                    {deleteMutation.isError && (
                        <p className="text-xsmall text-cancel">{t("deleteGoalModal.deleteFailed")}</p>
                    )}
                    {hasAttemptedSubmit && !selectedGoal && (
                        <p className="text-xsmall text-cancel">{t("deleteGoalModal.missingGoal")}</p>
                    )}
            </div>
            <div className="flex justify-end">
                    <Button label={t("common.cancel")} variant="cancel" onClick={resetAndClose} />
                    <Button
                        label={deleteMutation.isPending ? t("deleteGoalModal.deleting") : t("deleteGoalModal.delete")}
                        variant="primary"
                        onClick={handleDelete}
                        disabled={!selectedGoal || deleteMutation.isPending}
                    />
            </div>
        </Modal>
    )

}