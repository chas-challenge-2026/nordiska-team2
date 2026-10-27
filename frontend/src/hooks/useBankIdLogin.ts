import { useCallback, useEffect, useRef, useState } from "react"
import { useNavigate } from "react-router-dom"
import { apiClient } from "../client"
import { useAuth } from "./useAuth"

// BankID login: start an order, poll its status once per second, finish on complete or failed.
// Works the same against the mock and real BankID, since both backends answer the same contract.

export type BankIdFlowStatus = "idle" | "starting" | "pending" | "complete" | "failed"

// How long the user has to scan before BankID gives up on the order (per BankID's QR documentation).
export const BANKID_SCAN_WINDOW_SECONDS = 30
const POLL_INTERVAL_MS = 1000

interface StartResponse {
    orderRef: string
    autoStartToken: string
}

interface StatusResponse {
    status: "pending" | "complete" | "failed"
    hintCode?: string | null
    qrData?: string | null
    accessToken?: string
}

// Failure hint code -> translation key. Unknown codes get a generic message.
function failureMessage(hintCode: string | null): string {
    switch (hintCode) {
        case "startFailed": return "login.bankId.errors.startFailed"
        case "expiredTransaction": return "login.bankId.errors.expired"
        case "userCancel": return "login.bankId.errors.userCancel"
        case "certificateErr": return "login.bankId.errors.certificate"
        case "notACustomer": return "login.bankId.errors.notACustomer"
        default: return "login.bankId.errors.generic"
    }
}

// Same-device start: universal link on phones and tablets, the bankid:// scheme on computers.
export function bankIdAutoStartUrl(autoStartToken: string): string {
    const ua = navigator.userAgent
    const isMobile =
        /Android|iPhone|iPad|iPod/i.test(ua) ||
        (navigator.maxTouchPoints > 1 && /Macintosh/.test(ua)) // iPadOS reports itself as a Mac
    return isMobile
        ? `https://app.bankid.com/?autostarttoken=${autoStartToken}`
        : `bankid:///?autostarttoken=${autoStartToken}`
}

// Opens the BankID app the way BankID recommends: a link with referrerPolicy "origin", clicked
// from code. It must run soon after the user's click, or browsers block opening an external app.
export function openBankIdApp(autoStartToken: string) {
    const link = document.createElement("a")
    link.href = bankIdAutoStartUrl(autoStartToken)
    link.referrerPolicy = "origin"
    link.click()
}

export function useBankIdLogin() {
    const auth = useAuth()
    const navigate = useNavigate()

    const [status, setStatus] = useState<BankIdFlowStatus>("idle")
    const [qrData, setQrData] = useState<string | null>(null)
    const [hintCode, setHintCode] = useState<string | null>(null)
    const [autoStartToken, setAutoStartToken] = useState<string | null>(null)
    const [startedAt, setStartedAt] = useState<number | null>(null)
    const [errorMessage, setErrorMessage] = useState<string | null>(null)

    // Refs, not state: the polling loop must always see the CURRENT order,
    // not the value captured in whichever render started it.
    const orderRef = useRef<string | null>(null)
    const pollTimer = useRef<number | null>(null)

    const stopPolling = useCallback(() => {
        if (pollTimer.current !== null) {
            window.clearTimeout(pollTimer.current)
            pollTimer.current = null
        }
    }, [])

    const fail = useCallback((hint: string | null) => {
        orderRef.current = null
        setStatus("failed")
        setQrData(null)
        setHintCode(hint)
        setErrorMessage(failureMessage(hint))
    }, [])

    const poll = useCallback(async (ref: string) => {
        try {
            const { data } = await apiClient.get<StatusResponse>(`/auth/bankid/status/${ref}`)
            if (orderRef.current !== ref) return // cancelled or replaced while the request was in flight

            if (data.status === "pending") {
                setQrData(data.qrData ?? null)
                setHintCode(data.hintCode ?? null)
                // setTimeout after each answer (not setInterval), so slow responses never overlap.
                pollTimer.current = window.setTimeout(() => poll(ref), POLL_INTERVAL_MS)
                return
            }

            if (data.status === "complete" && data.accessToken) {
                orderRef.current = null
                setStatus("complete")
                auth.setAccessToken(data.accessToken)
                navigate("/dashboard")
                return
            }

            fail(data.hintCode ?? null)
        } catch {
            if (orderRef.current === ref) fail(null)
        }
    }, [auth, navigate, fail])

    const cancel = useCallback(async () => {
        stopPolling()
        const ref = orderRef.current
        orderRef.current = null
        setStatus("idle")
        setQrData(null)
        setHintCode(null)
        setAutoStartToken(null)
        setStartedAt(null)
        setErrorMessage(null)
        if (ref) {
            try {
                await apiClient.post(`/auth/bankid/cancel/${ref}`)
            } catch {
                // Best effort: the order expires on its own anyway.
            }
        }
    }, [stopPolling])

    const start = useCallback(async (): Promise<string | null> => {
        await cancel() // never leave an old order running behind a new one
        setStatus("starting")
        try {
            const { data } = await apiClient.post<StartResponse>("/auth/bankid/start")
            orderRef.current = data.orderRef
            setAutoStartToken(data.autoStartToken)
            setStartedAt(Date.now())
            setStatus("pending")
            poll(data.orderRef)
            return data.autoStartToken
        } catch {
            fail(null)
            return null
        }
    }, [cancel, poll, fail])

    // MOCK ONLY: plays the user's phone approving. The backend answers 404 unless the mock is active in Development.
    const simulateApproval = useCallback(async (personalId: string): Promise<boolean> => {
        const ref = orderRef.current
        if (!ref) return false
        try {
            await apiClient.post(`/auth/bankid/mock/approve/${ref}`, { personalId })
            return true
        } catch {
            return false
        }
    }, [])

    // Leaving the page mid-login: stop polling and cancel the order.
    useEffect(() => {
        return () => {
            stopPolling()
            const ref = orderRef.current
            orderRef.current = null
            if (ref) apiClient.post(`/auth/bankid/cancel/${ref}`).catch(() => {})
        }
    }, [stopPolling])

    return { status, qrData, hintCode, autoStartToken, startedAt, errorMessage, start, cancel, simulateApproval }
}