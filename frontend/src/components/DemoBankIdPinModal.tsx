import { useState } from "react";

/*
    DEMO ONLY -- this PIN is never checked, by anything, anywhere.
    It exists purely so the mock BankID flow visually resembles the real
    app's approve step. Any 6 digits are accepted.

    Note this is also how it would NOT work in reality: with real BankID
    the PIN is entered in the BankID app and verified by BankID's own
    infrastructure -- it never reaches our frontend or backend at all.
*/

type BankIdPinModalProps = {
    onConfirm: () => void;
    onCancel: () => void;
};

export default function BankIdPinModal({ onConfirm, onCancel }: BankIdPinModalProps) {
    const [pin, setPin] = useState('');

    const isComplete = pin.length === 6;

    function handleSubmit(e: React.FormEvent) {
        e.preventDefault();
        if (!isComplete) return;
        onConfirm(); // the pin value is deliberately not passed anywhere
    }

    return (
        <div
            className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"
            onClick={onCancel}
        >
            {/* stopPropagation so clicking inside the dialog doesn't
                trigger the backdrop's onCancel */}
            <div
                className="bg-white rounded-default p-6 w-full max-w-xs"
                onClick={(e) => e.stopPropagation()}
            >
                <h2 className="font-bold text-center">BankID</h2>
                <p className="text-muted text-small text-center mt-1">
                    Ange pin
                </p>

                <form onSubmit={handleSubmit} className="mt-4 space-y-4">
                    <input
                        type="password"
                        inputMode="numeric"
                        autoFocus
                        maxLength={6}
                        placeholder="••••••"
                        value={pin}
                        // Strips anything non-numeric as it's typed, so the
                        // 6-digit check can't be satisfied by letters.
                        onChange={(e) => setPin(e.target.value.replace(/\D/g, ''))}
                        className="border border-border rounded-default px-3 py-2 w-full text-center tracking-[0.5em]"
                    />

                    <div className="flex justify-center gap-2">
                        <button
                            type="submit"
                            disabled={!isComplete}
                            className="bg-brand hover:bg-brand/90 text-white rounded-default px-4 py-2 disabled:opacity-50"
                        >
                            Identifiera
                        </button>
                        <button
                            type="button"
                            onClick={onCancel}
                            className="border border-border rounded-default px-4 py-2"
                        >
                            Avbryt
                        </button>
                    </div>
                </form>

                <p className="text-muted text-center mt-4" style={{ fontSize: '0.7rem' }}>
                    Endast för demo - koden kontrolleras ej
                </p>
            </div>
        </div>
    );
}