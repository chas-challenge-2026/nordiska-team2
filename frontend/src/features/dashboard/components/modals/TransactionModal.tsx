import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import Button from "../../../../components/ui/Button";
import Modal from "../../../../components/ui/Modal"
import SelectOptions from "../../../../components/ui/Select";
import DepositWithdrawFields from "../../../../components/forms/DepositWithdrawFields";
import { useAccounts } from "../../../../hooks/useAccounts";
import type { OptionType } from "../../../../components/ui/Select";
import { useAlert } from "../../../../hooks/useAlert";
import { apiClient } from "../../../../client";
import { useTranslation } from "react-i18next";

type TransactionModalProps = {
    isOpen: boolean;
    onClose: () => void;
};

const amountFormatter = new Intl.NumberFormat("sv-SE", {
    style: "currency",
    currency: "SEK",
});

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
    const [mode, setMode] = useState<"deposit" | "withdraw">("deposit");
    const [hasAttemptedSubmit, setHasAttemptedSubmit] = useState(false);
    const [description, setDescription] = useState("");

    const amountValue = Number(amount);
    const accountData = accounts?.find((a) => String(a.id) === selectedAccount?.value)
    const isMissingFields = selectedAccount === null || amount.trim() === "";
    const isAmountInvalid = amount.trim() !== "" && (Number.isNaN(amountValue) || amountValue <= 0);
    const insufficientFunds = mode === "withdraw" && accountData !== undefined && amountValue > accountData.balance;

    const previewDepositBalance = (accountData?.balance ?? 0) + (Number.isNaN(amountValue) ? 0 : amountValue);
    const previewWithdrawBalance = (accountData?.balance ?? 0) - (Number.isNaN(amountValue) ? 0 : amountValue);

    function resetAndClose() {
        setSelectedAccount(null);
        setAmount("");
        setMode("deposit");
        setHasAttemptedSubmit(false);
        setDescription("");
        onClose();
    }

    const depositMutation = useMutation({
        mutationFn: () =>
            apiClient.post("/transactions/deposit", {
                accountId: Number(selectedAccount!.value), 
                amount: amountValue,
                description: description.trim() === "" ? undefined : description.trim(),
            }),
           onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ["accounts"] })
            queryClient.invalidateQueries({ queryKey: ["transactions", Number(selectedAccount!.value)] });
            queryClient.invalidateQueries({ queryKey: ["savings-goals"] });  
            showAlert({ type: "success", title: t("common.done"), message: t("transactionModal.depositDone") })
            resetAndClose();
        },
    });

    const withdrawMutation = useMutation({
        mutationFn: () =>
            apiClient.post("/transactions/withdraw", {
                accountId: Number(selectedAccount!.value), 
                amount: amountValue,
                description: description.trim() === "" ? undefined : description.trim(),
            }),
           onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ["accounts"] })
            queryClient.invalidateQueries({ queryKey: ["transactions", Number(selectedAccount!.value)] });
            queryClient.invalidateQueries({ queryKey: ["savings-goals"] }); 

            showAlert({ type: "success", title: t("common.done"), message: t("transactionModal.withdrawDone") })
            resetAndClose();
        },
    });

    function handleSubmit() {
        setHasAttemptedSubmit(true);
        if (isMissingFields || isAmountInvalid || insufficientFunds) return;
        if (mode === "deposit") depositMutation.mutate();
        else withdrawMutation.mutate();
    }

    return(
        <Modal title={t("transactionModal.title")}
                isOpen ={isOpen}
                onClose={resetAndClose} >
            <div className="flex flex-col gap-5 border border-border rounded-card
                        w-full h-full px-3 pt-3 pb-7"
            >
              <div className="z-20">  
                <p className="mb-1 mt-2 ml-1">{t("common.accountRequired")}</p>
                <SelectOptions 
                    value={selectedAccount} 
                    onChange={setSelectedAccount}
                    options={accountOptions}
                    placeholder={t("common.selectAccount")} />
                </div>
                <div>
                    <DepositWithdrawFields
                        mode={mode}
                        setMode={setMode}
                        amount={amount}
                        setAmount={setAmount}
                        amountLabel={<p className="mb-1 ml-1">{t("common.amountRequired")}</p>}
                        quickAmountsClassName="flex gap-3 mt-3"
                        description={description}
                        setDescription={setDescription}
                        descriptionLabel={t("transactionModal.descriptionLabel")}
                    />
                    {accountData && (
                        <p className="mt-2 ml-1 text-xsmall text-muted">
                            {mode === "deposit"
                                ? t("transactionModal.afterDeposit", { amount: amountFormatter.format(previewDepositBalance) })
                                : t("transactionModal.afterWithdraw", { amount: amountFormatter.format(previewWithdrawBalance) })}
                        </p>
                    )}
                </div>

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

            <div className="flex justify-end gap-3 mt-5">

                <Button
                    label={t("common.cancel")}
                    variant="cancel"
                    onClick={resetAndClose}
                />
                <Button
                    label={
                        mode === "deposit" 
                        ? depositMutation.isPending ? t("transactionModal.depositing") : t("transactionModal.deposit")
                        : withdrawMutation.isPending ? t("transactionModal.withdrawing") : t("transactionModal.withdraw") }
                    variant="success"
                    onClick={handleSubmit}
                    disabled={depositMutation.isPending || withdrawMutation.isPending}
                />
            </div>
        </Modal>
    )
}
