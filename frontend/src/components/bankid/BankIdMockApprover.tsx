import { useState } from "react"
import { useTranslation } from "react-i18next"
import BankIdPinModal from "../DemoBankIdPinModal"

// MOCK ONLY: stands in for the user's phone. Rendered only when VITE_BANKID_MOCK=true.
// The personal number is entered HERE (on the "phone"), not on the login page, just like real BankID.
interface Props {
    onApprove: (personalId: string) => Promise<boolean>
}

export default function BankIdMockApprover({ onApprove }: Props) {
    const { t } = useTranslation()
    const [personalId, setPersonalId] = useState("")
    const [showPin, setShowPin] = useState(false)
    const [error, setError] = useState<string | null>(null)

    // The PIN is deliberately not used anywhere; the modal is only visual.
    async function handlePinConfirm() {
        setShowPin(false)
        const ok = await onApprove(personalId.trim())
        if (!ok) setError("login.bankId.mock.approveFailed")
    }

    return (
        <div className="space-y-2 rounded-card border border-dashed border-border p-3">
            <p className="text-xsmall font-bold text-muted">{t("login.bankId.mock.title")}</p>
            <input
                type="text"
                value={personalId}
                onChange={(e) => setPersonalId(e.target.value)}
                placeholder={t("login.personalIdPlaceholder")}
                className="w-full rounded-card border border-border bg-card/70 px-3 py-2"
            />
            <button
                type="button"
                disabled={personalId.trim() === ""}
                onClick={() => { setError(null); setShowPin(true) }}
                className="w-full rounded-card border border-border px-4 py-2 disabled:opacity-50"
            >
                {t("login.bankId.mock.approve")}
            </button>
            {error && <p className="text-xsmall">{t(error)}</p>}
            {showPin && <BankIdPinModal onConfirm={handlePinConfirm} onCancel={() => setShowPin(false)} />}
        </div>
    )
}