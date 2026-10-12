import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import Button from "../../../../../components/ui/Button";
import InputField from "../../../../../components/ui/Input";
import Modal from "../../../../../components/ui/Modal"   
import SelectOptions from "../../../../../components/ui/Select";
import { useAccounts } from "../../../../../hooks/useAccounts";
import type { OptionType } from "../../../../../components/ui/Select";
import { useAlert } from "../../../../../hooks/useAlert";
import { apiClient } from "../../../../../client";
import { useTranslation } from "react-i18next";

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
    const { t } = useTranslation();

    const accountOptions: OptionType[] = accounts?.map((account) => ({
        value: String(account.id), 
        label: account.accountNumber 
    })) ?? []

    const [name, setName] = useState("");
    const [targetAmount, setTargetAmount] = useState("")
    const [account, setAccount] = useState<OptionType | null>(null);
    const [hasAttemptedSubmit, setHasAttemptedSubmit] = useState(false);

    const targetAmountValue = Number(targetAmount);
    const isMissingName = name.trim() === "";
    const isMissingAmount = targetAmount.trim() === "";
    const isAmountInvalid = !isMissingAmount && (Number.isNaN(targetAmountValue) || targetAmountValue <= 0);
    const isAccountMissing = account === null;

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
            showAlert({ type: "success", title: t("common.done"), message: t("createGoalModal.done") })
            resetAndClose();
        },
    });

    function handleCreate() {
        setHasAttemptedSubmit(true);
        if (isMissingName || isAccountMissing) return;
        createMutation.mutate();

    }
    
    return(
        <Modal title={t("createGoalModal.title")}
                isOpen ={isOpen}
                onClose={resetAndClose} >
            <div className="flex flex-col gap-1 border border-border rounded-default
                        w-full h-full p-3"
            >
                <p className="mt-2">{t("createGoalModal.nameRequired")}</p>
                <InputField 
                    value={name} 
                    onChange={setName}
                    placeholder={t("createGoalModal.namePlaceholder")} />

                {hasAttemptedSubmit && isMissingName && (
                    <p className="text-xsmall text-cancel">{t("createGoalModal.missingName")}</p>
                )}

                <p className="mt-2">{t("createGoalModal.targetRequired")}</p>
                    <InputField 
                        value={targetAmount}
                        onChange={setTargetAmount}
                        placeholder={t("common.amount")}
                        type="number" />

                {hasAttemptedSubmit && isMissingAmount && (
                    <p className="text-xsmall text-cancel">{t("createGoalModal.missingTarget")}</p>
                )}        

                {hasAttemptedSubmit && isAmountInvalid && (
                    <p className="text-xsmall text-cancel">{t("createGoalModal.targetPositive")}</p>
                )}
                <p className="mt-2">{t("createGoalModal.linkAccountRequired")}</p>
                <SelectOptions
                    value={account} 
                    onChange={setAccount}
                    options={accountOptions}
                    placeholder={t("common.selectAccount")}
                    />
                {hasAttemptedSubmit && isAccountMissing && (
                    <p className="text-xsmall text-cancel">{t("createGoalModal.missingAccount")}</p>
                )}

                {createMutation.isError && (
                    <p className="text-xsmall text-cancel">{t("common.genericError")}</p>
                )}
            </div>

            <div className="flex justify-end mt-10">
  
                <Button
                    label={t("common.cancel")}
                    variant="cancel"
                    onClick={resetAndClose}
                />
                <Button
                    label={createMutation.isPending ? t("createGoalModal.creating") : t("createGoalModal.create")}
                    variant="secondary"
                    onClick={handleCreate}
                    disabled={createMutation.isPending}
                />
            </div>
        </Modal>
    )
}
                    
                    
