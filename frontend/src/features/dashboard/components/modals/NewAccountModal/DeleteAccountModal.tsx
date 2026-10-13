import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import Button from "../../../../../components/ui/Button";
import Modal from "../../../../../components/ui/Modal";
import SelectOptions from "../../../../../components/ui/Select";
import type { OptionType } from "../../../../../components/ui/Select";
import { useAccounts } from "../../../../../hooks/useAccounts";
import { apiClient } from "../../../../../client";

type DeleteAccountModalProps = {
    isOpen: boolean;
    onClose: () => void;
};

export default function DeleteAccountModal({ isOpen, onClose }: DeleteAccountModalProps) {
    const queryClient = useQueryClient();
    const { data: accounts } = useAccounts();
    const [selectedAccount, setSelectedAccount] = useState<OptionType | null>(null);
    const [hasAttemptedSubmit, setHasAttemptedSubmit] = useState(false);

    const accountOptions: OptionType[] = accounts?.map((a) => ({
        value: String(a.id),
        label: a.accountNumber,
    })) ?? [];

    const deleteMutation = useMutation({
        mutationFn: (accountId: number) => apiClient.delete(`/accounts/${accountId}`),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ["accounts"] });
            resetAndClose();
        },
    });

    function resetAndClose() {
        setSelectedAccount(null);
        setHasAttemptedSubmit(false);
        onClose();
    }

    function handleDelete() {
        setHasAttemptedSubmit(true);
        if (!selectedAccount) return;
        deleteMutation.mutate(Number(selectedAccount.value));
    }

    return (
        <Modal title="Ta bort konto" isOpen={isOpen} onClose={resetAndClose}>
            <div className="flex flex-col gap-1 border border-border rounded-card w-full h-full px-4 py-6 mt-3 mb-10">
                <p className="mb-2 ml-1">Vilket konto vill du ta bort?</p>
                <SelectOptions value={selectedAccount} 
                                onChange={setSelectedAccount} 
                                options={accountOptions} 
                                placeholder="Välj konto" />
                {deleteMutation.isError && 
                    <p className="text-xsmall text-cancel">Kontot kunde inte tas bort (måste vara tomt).</p>}
                {hasAttemptedSubmit && !selectedAccount && 
                    <p className="text-xsmall text-cancel">Du måste välja ett konto.</p>}
            </div>
            <div className="flex justify-end gap-5">
                <Button label="Avbryt" variant="cancel" onClick={resetAndClose} />
                <Button
                    label={deleteMutation.isPending ? "Tar bort..." : "Ta bort"}
                    variant="secondary"
                    onClick={handleDelete}
                    disabled={!selectedAccount || deleteMutation.isPending}
                />
            </div>
        </Modal>
    );
}
