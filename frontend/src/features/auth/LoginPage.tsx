import { useState } from "react";
import Card from "../../components/cards/Card"
import { apiClient } from "../../client"
import { Eye, EyeOff } from "lucide-react"
import { useAuth } from "../../hooks/useAuth"
import { useNavigate } from "react-router-dom"
import heroImage from "../../assets/images/loginPage.png"
import checkIcon from "../../assets/svg/check-accent.svg"
import { useBankIdLogin, bankIdAutoStartUrl, openBankIdApp, BANKID_SCAN_WINDOW_SECONDS } from "../../hooks/useBankIdLogin"
import BankIdQrCode from "../../components/bankid/BankIdQrCode"
import BankIdMockApprover from "../../components/bankid/BankIdMockApprover"
import BlobBackground from "../../components/ui/BlobBackground"
import { useTranslation } from "react-i18next"
import LanguageSwitcher from "../../components/ui/LanguageSwitcher"


export default function LoginPage() {
    const [email, setEmail] = useState('')
    const [password, setPassword] = useState('')
    const [errorMessage, setErrorMessage] = useState('')
    const [isLoading, setIsLoading] = useState(false)
    const [showPassword, setShowPassword] = useState(false)
    const [mode, setMode] = useState<"email" | "bankId">("email");
    const { t } = useTranslation()  
    const auth = useAuth()
    const navigate = useNavigate()

    // --- BankID ---
    const bankId = useBankIdLogin()
    const [bankIdDevice, setBankIdDevice] = useState<"this" | "other">("other")
    const isBankIdMock = import.meta.env.VITE_BANKID_MOCK === "true"

    async function startBankId(device: "this" | "other") {
        setBankIdDevice(device)
        const autoStartToken = await bankId.start()
        // Same device: open the BankID app straight away. Never for the mock: its tokens are made up,
        // so the real BankID app would only show an error.
        if (device === "this" && autoStartToken && !isBankIdMock) {
            openBankIdApp(autoStartToken)
        }
    }

    // Recomputed on every render; the QR code updates every second, which drives the re-render.
    const secondsLeft = bankId.startedAt === null
        ? BANKID_SCAN_WINDOW_SECONDS
        : Math.max(0, BANKID_SCAN_WINDOW_SECONDS - Math.floor((Date.now() - bankId.startedAt) / 1000))
    // --- end BankID ---
    

       async function handleSubmit(e: React.FormEvent) {
                e.preventDefault()
                if (email === '' || password === '') {
                    setErrorMessage('login.errors.missingFields')
                } else {
                    setIsLoading(true)
                    try {
                    const response = await apiClient.post('/auth/login', { email, password })
                    auth.setAccessToken(response.data.accessToken)
                    navigate("/dashboard")
                    setIsLoading(false)
                } catch {
                    setErrorMessage('login.errors.invalidCredentials')
                    setIsLoading(false)
                }

            }

            }

    return ( 
        <div className="relative isolate grid min-h-screen lg:grid-cols-2"> 
            <BlobBackground />

            {/* Språkväljare uppe i högra hörnet */}
            <div className="absolute top-4 right-4 z-10">
                <LanguageSwitcher />
            </div>

            {/* ========== Vänster sida ========== */}
            {/* Panelen "flyter" som ett rundat kort, så att det inte blir någon hård kant mot högersidan */}
            <div className="relative overflow-hidden hidden lg:m-4 lg:rounded-card lg:p-16 lg:flex lg:flex-col lg:gap-15
                            shadow-2xl shadow-brand/20 dark:shadow-black/40
                            bg-linear-to-br from-brand to-[#0f3550]
                            dark:from-[#16405e] dark:to-sidebar">
                {/* Mjuka ljusfläckar i den blå panelen */}
                <div aria-hidden="true" className="pointer-events-none absolute -top-32 -left-24 size-96 rounded-full bg-sky-400/20 blur-3xl" />
                <div aria-hidden="true" className="pointer-events-none absolute bottom-0 right-1/4 size-80 rounded-full bg-accent/10 blur-3xl" />
                <h2 className="relative font-bold text-white">
                    nordiska<span className="text-accent">.</span>
                </h2>
                <div className="relative">
                    <h1 className="text-white text-page-title font-bold">
                        {t("login.heroTitle")}
                        <span className="block ml-auto">{t("login.heroTitleEnd")}</span>
                    </h1>
                    <p className="text-small text-white opacity-60 italic">
                        {t("login.heroSubtitle")}
                    </p>
                    <img src={heroImage} className="mt-10 mx-auto rounded-xl opacity-90
                                                    shadow-2xl shadow-black/30" mb-5/>
                    <div className="flex gap-3 mt-2">
                                    {["savingsGoal",
                                    "depositWithdraw",
                                    "faq",
                                    "taxReports"].map((feature) => (
                                        <div key={feature}
                                            className="flex items-center gap-2 text-xsmall text-white bg-white/20 rounded-card 
                                                        px-3 py-1">
                                                <img src={checkIcon} className="size-4"/>
                                                <span className="min-w-0 break-words">{t(`login.features.${feature}`)}</span>
                                        </div>
                                    ))}
                    </div>
                </div>
                
            </div>

            {/* ========== Höger sida ========== */}
            <div className="md:m-auto md:w-3/5 md:h-auto md:m-auto h-full w-full">   
                <Card 
                    headerContent={
                        <div className="mx-5 my-3 md:my-1">
                            <h2 className="text-title">{t("login.welcome")}</h2>
                            <p className="text-small text-white opacity-60">
                                {t("login.subtitle")}
                            </p>
                        </div>
                    } 
                    headerClassName="bg-linear-to-br from-brand to-[#0f3550] dark:from-[#16405e] dark:to-sidebar border-none"
                    className="md:min-h-125 h-full bg-card/85! backdrop-blur-md border-white/70! dark:border-toggle!
                               shadow-2xl! shadow-brand/15! dark:shadow-black/40!">
      
                <div className="flex flex-col justify-center h-full gap-6 md:gap-0 ">
                    <div className="relative bg-toggle flex rounded-card border border-toggle overflow-hidden md:m-5">
                        <div className={`absolute h-full inset-y-0 w-1/2 bg-white border-2 border-toggle rounded-card
                            transition-transform duration-500 ease-in-out
                            ${mode === "email" ? "translate-x-0" : "translate-x-full"}`} />
                        <button
                            type="button"
                            className={`relative z-10 flex-1 md:p-2 p-3 transition-colors text-small
                                        ${mode === "email" ? "text-brand" : "text-muted bg-toggle"}`}
                            onClick={() => { setMode("email"); bankId.cancel() }}>
                                {t("login.tabEmail")}
                        </button>
                        <button
                            type="button"
                            className={`relative z-10 flex-1 p-2 transition-colors text-small
                                        ${mode === "bankId" ? "text-brand" : "text-muted bg-toggle"}`}
                            onClick={() => setMode("bankId")}>
                                BankID
                        </button>
                    </div>

            {mode === "email" && (
                <div className="text-small px-5 pb-5">
                    <form onSubmit={handleSubmit}
                        className="flex flex-col gap-2 w-full">
                        <div>
                            <p className="text-xsmall ml-1 text-muted font-bold">{t("login.email")}</p>
                            <input type="email" placeholder={t("login.email")} value={email} onChange={(e) => setEmail(e.target.value)} className="border border-border rounded-default px-3 py-2 w-full bg-card/70 transition duration-300 focus:outline-none focus:border-brand-text focus:ring-4 focus:ring-brand/15"/>
                        </div>
                        
                        <div>
                            <p className="text-xsmall ml-1 text-muted font-bold">{t("login.password")}</p>
                            <div className="relative">
                                <input type={showPassword ? "text" : "password"} placeholder={t("login.password")} value={password} onChange={(e) => setPassword(e.target.value)} className="border border-border rounded-default px-3 py-2 w-full bg-card/70 transition duration-300 focus:outline-none focus:border-brand-text focus:ring-4 focus:ring-brand/15"/>
                                <button type="button" onClick={() => setShowPassword(!showPassword)} className="absolute right-3 top-1/2 -translate-y-1/2">
                                    {showPassword ? <Eye /> : <EyeOff />}
                                </button>
                            </div>
                        </div>

                        <div className="flex justify-center">
                        <button className="bg-brand hover:bg-brand/90 hover:shadow-lg hover:shadow-brand/25 transition duration-300 text-white font-bold rounded-default px-4 py-3 w-full mt-3">{isLoading ? t("login.submitting") : t("login.submit")}</button>
                        </div>
                        <div className="flex justify-center text-sm">
                        {errorMessage && <p>{t(errorMessage)}</p>}
                        </div>
                    
                    </form>
                </div>
            )}

            {mode === "bankId" && (
                <div className="text-small space-y-4 px-5 pb-5">
                    {(bankId.status === "idle" || bankId.status === "failed") && (
                        <div className="flex flex-col gap-3">
                            <button
                                type="button"
                                onClick={() => startBankId("this")}
                                className="bg-brand hover:bg-brand/90 hover:shadow-lg hover:shadow-brand/25 transition duration-300 text-white rounded-card px-4 py-3 w-full mt-3"
                            >
                                {t("login.bankIdSubmit")}
                            </button>
                            <button
                                type="button"
                                onClick={() => startBankId("other")}
                                className="bg-background hover:bg-white text-brand border border-border rounded-card px-4 py-3 w-full"
                            >
                                {t("login.bankIdOtherDevice")}
                            </button>
                            {bankId.errorMessage && <p className="text-center text-sm">{t(bankId.errorMessage)}</p>}
                        </div>
                    )}

                    {bankId.status === "starting" && <p className="text-center">{t("login.bankIdWaiting")}</p>}

                    {bankId.status === "pending" && (
                        <div className="flex flex-col gap-3">
                            {bankIdDevice === "other" && bankId.qrData && (
                                <>
                                    <BankIdQrCode value={bankId.qrData} />
                                    <p className="text-center">{t("login.bankId.scanInstruction")}</p>
                                    <p className="text-center text-xsmall text-muted">
                                        {t("login.bankId.secondsLeft", { count: secondsLeft })}
                                    </p>
                                </>
                            )}

                            {bankIdDevice === "this" && bankId.autoStartToken && !isBankIdMock && (
                                // A link the user taps, rather than opening the app automatically: some phones
                                // only follow app links that come straight from a tap.
                                <a
                                    href={bankIdAutoStartUrl(bankId.autoStartToken)}
                                    referrerPolicy="origin"
                                    className="bg-brand text-white text-center rounded-card px-4 py-3 w-full"
                                >
                                    {t("login.bankId.openApp")}
                                </a>
                            )}

                            {isBankIdMock && <BankIdMockApprover onApprove={bankId.simulateApproval} />}

                            <button type="button" onClick={bankId.cancel} className="border border-border rounded-card px-4 py-2">
                                {t("login.cancel")}
                            </button>
                        </div>
                    )}
                </div>
            )}

            <p className="mx-3 mt-5 pt-5 text-small border-t-2 border-border">
                {t("login.newCustomerText")}
            </p>
            </div>
            {/* end BankID */}

                </Card>
            </div>
        </div>
    )
}