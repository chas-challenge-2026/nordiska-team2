import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "../../../../client";
import Button from "../../../../components/ui/Button";
import InputField from "../../../../components/ui/Input";
import Modal from "../../../../components/ui/Modal"
import TextareaInput from "../../../../components/ui/TextareaInput";
import SelectOptions from "../../../../components/ui/Select";
import { useAccounts } from "../../../../hooks/useAccounts";
import type { OptionType } from "../../../../components/ui/Select";
import { useAlert } from "../../../../hooks/useAlert";
import { useTranslation } from "react-i18next";
import { apiErrorMessage } from "../../../../lib/apiErrorMessage";

type TransferProps = {
    isOpen: boolean;
    onClose: () => void;
};

export default function TransferModal({
    isOpen,
    onClose,
}: TransferProps) {
    const { data: accounts } = useAccounts(); // RIKTIG DATA

    const { showAlert } = useAlert();
    const { t } = useTranslation();
    const accountOptions: OptionType[] = accounts?.map((account) => ({
        value: String(account.id), label: account.accountNumber })) ?? []

    const [fromAccount, setFromAccount] = useState<OptionType | null>(null);
    const [toAccount, setToAccount] = useState<OptionType | null>(null);
    const [amount, setAmount] = useState("");
    const [message, setMessage] = useState("");
    const [hasAttemptedSubmit, setHasAttemptedSubmit] = useState(false);
    const queryClient = useQueryClient();
    const transferMutation = useMutation ({
        mutationFn: () =>
            apiClient.post("/transactions/transfer", {
                fromAccountId: Number(fromAccount!.value),
                toAccountId: Number(toAccount!.value),
                amount: amountValue,
                description: message.trim() === "" ? undefined : message.trim(), 
            }),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ["accounts"] });
            queryClient.invalidateQueries({ queryKey: ["transactions", Number(fromAccount!.value)] });
            queryClient.invalidateQueries({ queryKey: ["transactions", Number(toAccount!.value)] })
            queryClient.invalidateQueries({ queryKey: ["savings-goals"] });

            showAlert({ type: "success", title: t("common.done"), message: t("transferModal.done") });
            resetAndClose();   
        }
    })

    const amountValue = Number(amount);
    const fromAccountData = accounts?.find((account) => String(account.id) === fromAccount?.value);
    const insufficientFunds = fromAccountData !== undefined && amountValue > fromAccountData.balance;
    const isMissingFields = fromAccount === null || toAccount === null || amount.trim() === "";
    const sameAccount = fromAccount !== null && toAccount !== null && fromAccount.value === toAccount.value;
    const isAmountInvalid = amount.trim() !== "" && (Number.isNaN(amountValue) || amountValue <= 0);
    const isValid = !isMissingFields && !sameAccount && !isAmountInvalid && !insufficientFunds;
      
    function resetAndClose() {
        transferMutation.reset();
        setFromAccount(null);
        setToAccount(null);
        setAmount("")
        setMessage("");
        setHasAttemptedSubmit(false);
        onClose();
    }

    function handleConfirmed() {
    setHasAttemptedSubmit(true);
    if (!isValid || !fromAccount || !toAccount) return;
    transferMutation.mutate();  
    }

    return(
        <Modal title={t("transferModal.title")}
                isOpen ={isOpen}
                onClose={resetAndClose}
        >
            <div className="flex flex-col gap-3 border border-border rounded-card
                        w-full h-full px-3 pt-3 pb-7"
            >
               <div> <p className="mb-1 mt-2 ml-1">{t("transferModal.fromAccountRequired")}</p>
                <SelectOptions
                    value={fromAccount}
                    onChange={setFromAccount}
                    options={accountOptions}
                    placeholder={t("transferModal.fromAccount")} />
              
                {hasAttemptedSubmit && insufficientFunds && (
                    <p className="text-xsmall text-cancel">{t("transferModal.insufficientFunds")}</p>
                )} </div>
                 <div>
                <p className="mb-1 mt-2 ml-1">{t("transferModal.toAccountRequired")}</p>
                <SelectOptions
                    value={toAccount}
                    onChange={setToAccount}
                    options={accountOptions}
                    placeholder={t("transferModal.toAccount")} />
              </div>
              <div>
                <p className="mb-1 mt-2 ml-1">{t("common.amountRequired")}</p>
                    <InputField
                        value={amount}
                        onChange={setAmount}
                        placeholder={t("common.amount")}/>
                
                {hasAttemptedSubmit && isAmountInvalid && (
                    <p className="text-xsmall text-cancel">{t("transferModal.amountNumber")}</p>
                )}
              </div>
              <div>
                <p className="mb-1 mt-2 ml-">{t("transferModal.messageLabel")}</p>
                <TextareaInput
                    placeholder={t("transferModal.messagePlaceholder")}
                    value={message}
                    onChange={setMessage}/>
              </div>
             
            </div>
            <p className="w-full text-xsmall text-right pr-1">{t("common.requiredFieldsNote")}</p>

            <div className="text-xsmall text-cancel text-right pr-1 mb-1">
                {hasAttemptedSubmit && sameAccount && (
                    <p>{t("transferModal.sameAccount")}</p>
                )}
                {hasAttemptedSubmit && isMissingFields && (
                    <p>{t("common.missingRequired")}</p>
                )}
                {transferMutation.isError && (
                    <p role="alert" className="text-xsmall text-cancel">{apiErrorMessage(transferMutation.error, t)}</p>
                )}
            </div>

            <div className="flex justify-end gap-3 mt-5">

                <Button
                    label={t("common.cancel")}
                    variant="cancel"
                    onClick={resetAndClose}

                />
                <Button
                    label={t("transferModal.send")}
                    variant="success"
                    onClick={handleConfirmed}
                    disabled={transferMutation.isPending}
                />
            </div>
        
        </Modal>
    )
}
