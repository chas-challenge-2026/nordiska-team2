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
            showAlert({ type: "success", title: "Klart!", message: "Insättningen är genomförd." })
            resetAndClose();
        },
    });

    
    const withdrawMutation = useMutation({
        mutationFn: () =>
            apiClient.post("/transactions/withdraw", {accountId: Number(selectedAccount!.value), amount: amountValue}),
           onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ["accounts"] })
            queryClient.invalidateQueries({ queryKey: ["transactions", Number(selectedAccount!.value)] });
            showAlert({ type: "success", title: "Klart!", message: "Uttaget är genomförd." })
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
        <Modal title="Insättning/Överföring"
                isOpen ={isOpen}
                onClose={resetAndClose} >
            <div className="flex flex-col gap-1 border border-border rounded-default
                        w-full h-full p-3"
            >
                <p className="mt-2">Konto*</p>
                <SelectOptions 
                    value={selectedAccount} 
                    onChange={setSelectedAccount}
                    options={accountOptions}
                    placeholder="Välj konto" />

                <p className="mt-2">Belopp*</p>
                    <InputField 
                        value={amount}
                        onChange={setAmount}
                        placeholder="Belopp"
                        type="number" />

                {hasAttemptedSubmit && isAmountInvalid && (
                    <p className="text-xsmall text-cancel">Beloppet måste vara en siffra större än 0.</p>
                )}
                {hasAttemptedSubmit && insufficientFunds && (
                    <p className="text-xsmall text-cancel">Otillräckligt saldo för uttag.</p>
                )}
                {hasAttemptedSubmit && isMissingFields && (
                    <p className="text-xsmall text-cancel">Du måste fylla i alla obligatoriska fält.</p>
                )}
                {(depositMutation.isError || withdrawMutation.isError) && (
                    <p className="text-xsmall text-cancel">Ett fel uppstod. Försök igen.</p>
                )}
            </div>

            <p className="w-full text-xsmall text-right pr-1">* Obligatoriska fält.</p>

            <div className="flex justify-end">
  
                <Button
                    label="Avbryt"
                    variant="cancel"
                    onClick={resetAndClose}
                />
                <Button
                    label={withdrawMutation.isPending ? "Tar ut ..." : "Ta ut"}
                    variant="secondary"
                    onClick={handleWithdraw}
                    disabled={withdrawMutation.isPending || depositMutation.isPending}
                />
                <Button
                    label={depositMutation.isPending ? "Sätter in ..." : "Sätt in"}
                    variant="success"
                    onClick={handleDeposit}
                    disabled={depositMutation.isPending || withdrawMutation.isPending}
                />
            </div>
        </Modal>
    )
}
                    
                    
