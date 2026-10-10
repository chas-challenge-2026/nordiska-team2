import { useState } from "react";
import Button from "../../../../../components/ui/Button";
import Modal from "../../../../../components/ui/Modal";
import SelectOptions from "../../../../../components/ui/Select";
import type { OptionType } from "../../../../../components/ui/Select";
import { useSavingsGoals, useDeleteSavingsGoals } from "../../../../../hooks/useSavingsGoals";

type DeleteSavingsGoalModalProps = {
    isOpen: boolean;
    onClose: () => void;
};

export default function DeleteSavingsGoalMoal({ isOpen, onClose }: DeleteSavingsGoalModalProps) {
    const { data: goals } = useSavingsGoals(); 
    const deleteMutation = useDeleteSavingsGoals();

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
        <Modal title ="Ta bort sparmål" 
                isOpen={isOpen}
                onClose={resetAndClose}>

            <div className="flex flex-col gap-1 
                            border border-border rounded-card 
                            w-full h-full px-4 py-6 mt-3 mb-10"> 
                <p className="mt-2">Vilket sparmål vill du ta bort?</p>
                    <SelectOptions
                        value={selectedGoal}
                        onChange={setSelectedGoal}
                        options={goalOptions}
                        placeholder="Välj sparmål"
                    />
                    {goalOptions.length === 0 && (
                        <p className="text-xsmall text-muted">Inga sparmål att ta bort.</p>
                    )}
                    {deleteMutation.isError && (
                        <p className="text-xsmall text-cancel">Kunde inte ta bort sparmålet.</p>
                    )}
                    {hasAttemptedSubmit && !selectedGoal && (
                        <p className="text-xsmall text-cancel">Du måste välja ett sparmål</p>
                    )}
            </div>
            <div className="flex justify-end gap-5">
                    <Button label="Avbryt" variant="cancel" onClick={resetAndClose} />
                    <Button
                        className="min-w-30"
                        label={deleteMutation.isPending ? "Tar bort..." : "Ta bort"}
                        variant="primary"
                        onClick={handleDelete}
                        disabled={!selectedGoal || deleteMutation.isPending}
                    />
            </div>
        </Modal>
    )

}