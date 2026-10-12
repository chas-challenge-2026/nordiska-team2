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
import { useTranslation } from "react-i18next";

type TransactionModalProps = {
    isOpen: boolean;
    onClose: () => void;
};

export default function TransactionModal({
    isOpen,
    onClose,
}: TransactionModalProps) {
    const queryClient = useQueryClient()
    const { data: accounts } = useAccounts();
    const { showAlert } = useAlert();
    const { t } = useTranslation();

    const accountOptions: OptionType[] = accounts?.map((account) => ({
        value: String(account.id), 
        label: account.accountNumber 
    })) ?? []

    const [selectedAccount, setSelectedAccount] = useState<OptionType | null>(null);
    const [amount, setAmount] = useState("");
    const [hasAttemptedSubmit, setHasAttemptedSubmit] = useState(false);

    const amountValue = Number(amount);
    const accountData = accounts?.find((a) => String(a.id) === selectedAccount?.value)
    const isMissingFields = selectedAccount === null || amount.trim() === "";
    const isAmountInvalid = amount.trim() !== "" && (Number.isNaN(amountValue) || amountValue <= 0);
    const insufficientFunds = accountData !== undefined && amountValue > accountData.balance;

    function resetAndClose() {
        setSelectedAccount(null);
        setAmount("");
        setHasAttemptedSubmit(false);
        onClose();
    }

    const depositMutation = useMutation({
        mutationFn: () =>
            apiClient.post("/transactions/deposit", {accountId: Number(selectedAccount!.value), amount: amountValue}),
           onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ["accounts"] })
            queryClient.invalidateQueries({ queryKey: ["transactions", Number(selectedAccount!.value)] });
            showAlert({ type: "success", title: t("common.done"), message: t("transactionModal.depositDone") })
            resetAndClose();
        },
    });

    
    const withdrawMutation = useMutation({
        mutationFn: () =>
            apiClient.post("/transactions/withdraw", {accountId: Number(selectedAccount!.value), amount: amountValue}),
           onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ["accounts"] })
            queryClient.invalidateQueries({ queryKey: ["transactions", Number(selectedAccount!.value)] });
            showAlert({ type: "success", title: t("common.done"), message: t("transactionModal.withdrawDone") })
            resetAndClose();
        },
    });

    function handleDeposit() {
        setHasAttemptedSubmit(true);
        if (isMissingFields || isAmountInvalid) return;
        depositMutation.mutate();
    }

    function handleWithdraw() {
        setHasAttemptedSubmit(true);
        if (isMissingFields || isAmountInvalid || insufficientFunds) return;
        withdrawMutation.mutate();

    }
    
    return(
        <Modal title={t("transactionModal.title")}
                isOpen ={isOpen}
                onClose={resetAndClose} >
            <div className="flex flex-col gap-1 border border-border rounded-default
                        w-full h-full p-3"
            >
                <p className="mt-2">{t("common.accountRequired")}</p>
                <SelectOptions 
                    value={selectedAccount} 
                    onChange={setSelectedAccount}
                    options={accountOptions}
                    placeholder={t("common.selectAccount")} />

                <p className="mt-2">{t("common.amountRequired")}</p>
                    <InputField 
                        value={amount}
                        onChange={setAmount}
                        placeholder={t("common.amount")}
                        type="number" />

                {hasAttemptedSubmit && isAmountInvalid && (
                    <p className="text-xsmall text-cancel">{t("transactionModal.amountPositive")}</p>
                )}
                {hasAttemptedSubmit && insufficientFunds && (
                    <p className="text-xsmall text-cancel">{t("transactionModal.insufficientFunds")}</p>
                )}
                {hasAttemptedSubmit && isMissingFields && (
                    <p className="text-xsmall text-cancel">{t("common.missingRequired")}</p>
                )}
                {(depositMutation.isError || withdrawMutation.isError) && (
                    <p className="text-xsmall text-cancel">{t("common.genericError")}</p>
                )}
            </div>

            <p className="w-full text-xsmall text-right pr-1">{t("common.requiredFieldsNote")}</p>

            <div className="flex justify-end">
  
                <Button
                    label={t("common.cancel")}
                    variant="cancel"
                    onClick={resetAndClose}
                />
                <Button
                    label={withdrawMutation.isPending ? t("transactionModal.withdrawing") : t("transactionModal.withdraw")}
                    variant="secondary"
                    onClick={handleWithdraw}
                    disabled={withdrawMutation.isPending || depositMutation.isPending}
                />
                <Button
                    label={depositMutation.isPending ? t("transactionModal.depositing") : t("transactionModal.deposit")}
                    variant="success"
                    onClick={handleDeposit}
                    disabled={depositMutation.isPending || withdrawMutation.isPending}
                />
            </div>
        </Modal>
    )
}
                    
                    
