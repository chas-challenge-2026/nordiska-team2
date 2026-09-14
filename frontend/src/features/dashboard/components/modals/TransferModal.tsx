import { useState } from "react";
import Button from "../../../../components/ui/Button";
import InputField from "../../../../components/ui/Input";
import Modal from "../../../../components/ui/Modal"  
import TextareaInput from "../../../../components/ui/TextareaInput";  
import SelectOptions from "../../../../components/ui/Select";
// import { useAccounts } from "../../../../hooks/useAccounts";
import { accounts as mockAccounts } from "../../data/accounts"; // TILLFÄLLIGT
import type { TransferType } from "../../../../types/transfer";
import type { OptionType } from "../../../../components/ui/Select";
import { useAlert } from "../../../../hooks/useAlert";

type TransferProps = {
    isOpen: boolean;
    onClose: () => void;
};

export default function TransferModal({
    isOpen,
    onClose,
}: TransferProps) {
    // const { data: accounts } = useAccounts();
    const accounts = mockAccounts; // TILLFÄLLIGT
    const { showAlert } = useAlert();
    const accountOptions: OptionType[] = accounts?.map((account) => ({
        value: String(account.id), label: account.name })) ?? []

    const [fromAccount, setFromAccount] = useState<OptionType | null>(null);
    const [toAccount, setToAccount] = useState<OptionType | null>(null);
    const [amount, setAmount] = useState("");
    const [message, setMessage] = useState("");

    const [hasAttemptedSubmit, setHasAttemptedSubmit] = useState(false);
    const amountValue = Number(amount);
    const fromAccountData = accounts.find((account) => String(account.id) === fromAccount?.value);
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
        showAlert({ type: "success", title: "Klart", message: "Överföringen är klart." });
        onClose();
    }
    
    return(
        <Modal title="Överföring"
                isOpen ={isOpen}
                onClose={onClose}
        >
            <div className="flex flex-col gap-1 border border-border rounded-default
                        w-full h-full p-3"
            >
                <p className="mt-2">Från konto*</p>
                <SelectOptions 
                    value={fromAccount} 
                    onChange={setFromAccount}
                    options={accountOptions}
                    placeholder="Från konto" />

                {hasAttemptedSubmit && insufficientFunds && (
                    <p className="text-xsmall text-cancel">Otillräckligt saldo på kontot.</p>
                )}

                <p className="mt-2">Till konto*</p>
                <SelectOptions 
                    value={toAccount} 
                    onChange={setToAccount}
                    options={accountOptions}
                    placeholder="Till konto" />

                <p className="mt-2">Belopp*</p>
                    <InputField 
                        value={amount}
                        onChange={setAmount}
                        placeholder="Belopp"/>
                {hasAttemptedSubmit && isAmountInvalid && (
                    <p className="text-xsmall text-cancel">Beloppet måste vara en siffra.</p>
                )}

                <p className="mt-2">OCR/Meddelande till mottagaren</p>
                <TextareaInput 
                    placeholder="OCR/Meddelande"
                    value={message}
                    onChange={setMessage}/>
            </div>
            <p className="w-full text-xsmall text-right pr-1">* Obligatoriska fält.</p>

            <div className="text-xsmall text-cancel text-right pr-1 mb-1">
                {hasAttemptedSubmit && sameAccount && (
                    <p>Överföring kan inte ske till och från samma konto.</p>
                )}
                {hasAttemptedSubmit && isMissingFields && (
                    <p>Du måste fylla i alla obligatoriska fält.</p>
                )}
            </div>

            <div className="flex justify-end">
  
                <Button
                    label="Avbryt"
                    variant="cancel"
                    onClick={onClose}
                />
                <Button
                    label="Skicka"
                    variant="success"
                    onClick={handleConfirmed}
                />
            </div>
        </Modal>
    )
}
                    
                    
