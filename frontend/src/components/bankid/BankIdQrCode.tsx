import { useEffect, useState } from "react"
import { QRCodeSVG } from "qrcode.react"
import { useTranslation } from "react-i18next"

// BankID guidance: the QR code is a <button> that opens full screen, has alt text saying it's a
// clickable QR code, uses low error correction (read from a screen), and is always black on white.
export default function BankIdQrCode({ value }: { value: string }) {
    const { t } = useTranslation()
    const [fullscreen, setFullscreen] = useState(false)

    useEffect(() => {
        if (!fullscreen) return
        const onKey = (e: KeyboardEvent) => { if (e.key === "Escape") setFullscreen(false) }
        window.addEventListener("keydown", onKey)
        return () => window.removeEventListener("keydown", onKey)
    }, [fullscreen])

    return (
        <>
            <button
                type="button"
                onClick={() => setFullscreen(true)}
                aria-label={t("login.bankId.qrAlt")}
                className="mx-auto block rounded-default bg-white p-3 shadow-md"
            >
                <QRCodeSVG value={value} size={200} level="L" marginSize={2} bgColor="#ffffff" fgColor="#000000" />
            </button>

            {fullscreen && (
                <div role="dialog" aria-modal="true"
                     className="fixed inset-0 z-50 flex flex-col items-center justify-center gap-6 bg-white">
                    <QRCodeSVG
                        value={value}
                        size={Math.min(window.innerWidth, window.innerHeight) * 0.7}
                        level="L" marginSize={4} bgColor="#ffffff" fgColor="#000000"
                    />
                    <button type="button" autoFocus onClick={() => setFullscreen(false)}
                            className="rounded-default border border-black px-6 py-2 text-black">
                        {t("login.bankId.closeFullscreen")}
                    </button>
                </div>
            )}
        </>
    )
}