import type { ReactNode } from "react";
import InputField from "../ui/Input";
import Button from "../ui/Button";
import TextareaInput from "../ui/TextareaInput";
import { useTranslation } from "react-i18next";

type DepositWithdrawFieldsProps = {
    mode: "deposit" | "withdraw";
    setMode: (mode: "deposit" | "withdraw") => void;
    amount: string;
    setAmount: (value: string) => void;
    amountLabel?: ReactNode;
    amountInputClassName?: string;
    quickAmountsClassName?: string;
    description: string;
    setDescription: (value: string) => void;
    descriptionLabel?: ReactNode;
};

export default function DepositWithdrawFields({
    mode,
    setMode,
    amount,
    setAmount,
    amountLabel,
    amountInputClassName = "",
    quickAmountsClassName = "flex gap-3 mt-2",
    description,
    setDescription,
    descriptionLabel,
}: DepositWithdrawFieldsProps) {
    const { t } = useTranslation();
    return (
        <>
            <div className="relative bg-toggle flex rounded-card border border-toggle overflow-hidden mb-4">
                <div className={`absolute inset-y-0 w-1/2 bg-white border-2 border-toggle rounded-card
                    transition-transform duration-500 ease-in-out
                    ${mode === "deposit" ? "translate-x-0" : "translate-x-full"}`} />
                <button
                    type="button"
                    className={`relative z-5 flex-1 p-2 transition-colors
                                ${mode === "deposit" ? "text-brand" : "text-muted bg-toggle"}`}
                    onClick={() => setMode("deposit")}>
                        {t("depositWithdraw.deposit")}
                </button>
                <button
                    type="button"
                    className={`relative z-5 flex-1 p-2 transition-colors
                                ${mode === "withdraw" ? "text-brand" : "text-muted bg-toggle"}`}
                    onClick={() => setMode("withdraw")}>
                        {t("depositWithdraw.withdraw")}
                </button>
            </div>

            {amountLabel}
            <InputField className={amountInputClassName}
                placeholder={t("common.amount")}
                value={amount}
                onChange={setAmount}
                type="number"
            />

            <div className={`${quickAmountsClassName} mb-5`}>
                {[100, 500, 1000].map((present) => (
                    <Button
                        key={present}
                        label={t("depositWithdraw.presetAmount", { amount: present })}
                        variant="quickPick"
                        onClick={() => setAmount(String(present))}
                        />
                ))}
            </div>
            <div className="flex flex-col gap-1 mt-3">
                <span className="ml-1">{descriptionLabel}</span>
                <TextareaInput 
                    placeholder={t("depositWithdraw.descriptionPlaceholder")}
                    value={description}
                    onChange={setDescription}/>
                </div>
        </>
    );
}
