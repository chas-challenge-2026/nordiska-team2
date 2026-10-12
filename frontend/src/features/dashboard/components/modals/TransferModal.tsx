import { useState } from "react";
import Button from "../../../../components/ui/Button";
import InputField from "../../../../components/ui/Input";
import Modal from "../../../../components/ui/Modal"
import TextareaInput from "../../../../components/ui/TextareaInput";
import SelectOptions from "../../../../components/ui/Select";
import { useAccounts } from "../../../../hooks/useAccounts";
import type { TransferType } from "../../../../types/transfer";
import type { OptionType } from "../../../../components/ui/Select";
import { useAlert } from "../../../../hooks/useAlert";
import { useTranslation } from "react-i18next";

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
    const amountValue = Number(amount);
    const fromAccountData = accounts?.find((account) => String(account.id) === fromAccount?.value);
    const insufficientFunds = fromAccountData !== undefined && amountValue > fromAccountData.balance;
    const isMissingFields = fromAccount === null || toAccount === null || amount.trim() === "";
    const sameAccount = fromAccount !== null && toAccount !== null && fromAccount.value === toAccount.value;
    const isAmountInvalid = amount.trim() !== "" && (Number.isNaN(amountValue) || amountValue <= 0);
    const isValid = !isMissingFields && !sameAccount && !isAmountInvalid && !insufficientFunds;

    function handleConfirmed() {
        setHasAttemptedSubmit(true);
        if (!isValid || !fromAccount || !toAccount) return;

        const payload: TransferType = {
            fromAccountId: fromAccount.value,
            toAccountId: toAccount.value,
            amount: amountValue,
            description: message,
        };
        console.log("Redo att skicka:", payload);
        showAlert({ type: "success", title: t("common.done"), message: t("transferModal.done") });
        onClose();
    }

    return(
        <Modal title={t("transferModal.title")}
                isOpen ={isOpen}
                onClose={onClose}
        >
            <div className="flex flex-col gap-1 border border-border rounded-default
                        w-full h-full p-3"
            >
                <p className="mt-2">{t("transferModal.fromAccountRequired")}</p>
                <SelectOptions
                    value={fromAccount}
                    onChange={setFromAccount}
                    options={accountOptions}
                    placeholder={t("transferModal.fromAccount")} />

                {hasAttemptedSubmit && insufficientFunds && (
                    <p className="text-xsmall text-cancel">{t("transferModal.insufficientFunds")}</p>
                )}

                <p className="mt-2">{t("transferModal.toAccountRequired")}</p>
                <SelectOptions
                    value={toAccount}
                    onChange={setToAccount}
                    options={accountOptions}
                    placeholder={t("transferModal.toAccount")} />

                <p className="mt-2">{t("common.amountRequired")}</p>
                    <InputField
                        value={amount}
                        onChange={setAmount}
                        placeholder={t("common.amount")}/>
                {hasAttemptedSubmit && isAmountInvalid && (
                    <p className="text-xsmall text-cancel">{t("transferModal.amountNumber")}</p>
                )}

                <p className="mt-2">{t("transferModal.messageLabel")}</p>
                <TextareaInput
                    placeholder={t("transferModal.messagePlaceholder")}
                    value={message}
                    onChange={setMessage}/>
            </div>
            <p className="w-full text-xsmall text-right pr-1">{t("common.requiredFieldsNote")}</p>

            <div className="text-xsmall text-cancel text-right pr-1 mb-1">
                {hasAttemptedSubmit && sameAccount && (
                    <p>{t("transferModal.sameAccount")}</p>
                )}
                {hasAttemptedSubmit && isMissingFields && (
                    <p>{t("common.missingRequired")}</p>
                )}
            </div>

            <div className="flex justify-end">

                <Button
                    label={t("common.cancel")}
                    variant="cancel"
                    onClick={onClose}
                />
                <Button
                    label={t("transferModal.send")}
                    variant="success"
                    onClick={handleConfirmed}
                />
            </div>
        </Modal>
    )
}
