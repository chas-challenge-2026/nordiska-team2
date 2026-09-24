import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import Button from "../../../../components/ui/Button";
import InputField from "../../../../components/ui/Input";
import Modal from "../../../../components/ui/Modal"   
import SelectOptions from "../../../../components/ui/Select";
import { useAccounts } from "../../../../hooks/useAccounts";
import type { OptionType } from "../../../../components/ui/Select";
import { useAlert } from "../../../../hooks/useAlert";
import { apiClient } from "../../../../client";

type CreateSavingsGoalsProps = {
    isOpen: boolean;
    onClose: () => void;
};

export default function CreateSavingsGoalsModal({
    isOpen,
    onClose,
}: CreateSavingsGoalsProps) {
    const queryClient = useQueryClient()
    const { data: accounts } = useAccounts();
    const { showAlert } = useAlert();

    const accountOptions: OptionType[] = accounts?.map((account) => ({
        value: String(account.id), 
        label: account.accountNumber 
    })) ?? []

    const [name, setName] = useState("");
    const [targetAmount, setTargetAmount] = useState("")
    const [account, setAccount] = useState<OptionType | null>(null);
    const [hasAttemptedSubmit, setHasAttemptedSubmit] = useState(false);

    const targetAmountValue = Number(targetAmount);
    const isMissingFields = name.trim() === "" || targetAmount.trim() === "";
    const isAmountInvalid = targetAmount.trim() !== "" && (Number.isNaN(targetAmountValue) || targetAmountValue <= 0) 

    function resetAndClose() {
        setName("");
        setTargetAmount("");
        setAccount(null);
        setHasAttemptedSubmit(false);
        onClose();
    }

    const createMutation = useMutation({
        mutationFn: () =>
            apiClient.post("/savings-goals", {name, targetAmount: targetAmountValue, accountId: account ? Number(account.value) : null, }),
            onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ["savings-goals"] })
            showAlert({ type: "success", title: "Klart!", message: "Sparmål är skapad." })
            resetAndClose();
        },
    });

    function handleCreate() {
        setHasAttemptedSubmit(true);
        if (isMissingFields || isAmountInvalid) return;
        createMutation.mutate();

    }
    
    return(
        <Modal title="Nytt sparmål"
                isOpen ={isOpen}
                onClose={resetAndClose} >
            <div className="flex flex-col gap-1 border border-border rounded-default
                        w-full h-full p-3"
            >
                <p className="mt-2">Namn*</p>
                <InputField 
                    value={name} 
                    onChange={setName}
                    placeholder="Ex: Resa till Japan" />

                <p className="mt-2">Målbelopp*</p>
                    <InputField 
                        value={targetAmount}
                        onChange={setTargetAmount}
                        placeholder="Belopp"
                        type="number" />

                {hasAttemptedSubmit && isAmountInvalid && (
                    <p className="text-xsmall text-cancel">Målbeloppet måste vara en siffra större än 0.</p>
                )}
                <p className="mt-2">Koppla till konto*</p>
                <SelectOptions
                    value={account} 
                    onChange={setAccount}
                    options={accountOptions}
                    placeholder="Välj Konto"
                    />

                {hasAttemptedSubmit && isMissingFields && (
                    <p className="text-xsmall text-cancel">Du måste fylla i namn och målbelopp.</p>
                )}
                {createMutation.isError && (
                    <p className="text-xsmall text-cancel">Ett fel uppstod. Försök igen.</p>
                )}
            </div>

            <div className="flex justify-end">
  
                <Button
                    label="Avbryt"
                    variant="cancel"
                    onClick={resetAndClose}
                />
                <Button
                    label={createMutation.isPending ? "Skapar ..." : "Skapa sparmål"}
                    variant="secondary"
                    onClick={handleCreate}
                    disabled={createMutation.isPending}
                />
            </div>
        </Modal>
    )
}
                    
                    
