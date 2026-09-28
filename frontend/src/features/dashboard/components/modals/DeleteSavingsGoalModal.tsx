import { useState } from "react";
import Button from "../../../../components/ui/Button";
import Modal from "../../../../components/ui/Modal";
import SelectOptions from "../../../../components/ui/Select";
import type { OptionType } from "../../../../components/ui/Select";
import { useSavingsGoals, useDeleteSavingsGoals } from "../../../../hooks/useSavingsGoals";

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
    
    function resetAndClose() {
        setSelectedGoal(null)
        onClose()
    }

    function handleDelete() {
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
                            border border-border rounded-default 
                            w-full h-full p-3"> 
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
            </div>
            <div className="flex justify-end">
                    <Button label="Avbryt" variant="cancel" onClick={resetAndClose} />
                    <Button
                        label={deleteMutation.isPending ? "Tar bort..." : "Ta bort"}
                        variant="cancel"
                        onClick={handleDelete}
                        disabled={!selectedGoal || deleteMutation.isPending}
                    />
            </div>
        </Modal>
    )

}