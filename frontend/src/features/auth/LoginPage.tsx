import { useState } from "react";
import Card from "../../components/cards/Card"
import { apiClient } from "../../client"
import { Eye, EyeOff } from "lucide-react"
import { useAuth } from "../../hooks/useAuth"
import { useNavigate } from "react-router-dom"
import { useBankIdLogin } from "../../hooks/useBankIdLogin"
import heroImage from "../../assets/images/loginPage.png"
import checkIcon from "../../assets/svg/check-accent.svg"
import BankIdPinModal from "../../components/DemoBankIdPinModal"
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

    // --- BankID (new) ---
    const [personalId, setPersonalId] = useState('')
    const [personalIdError, setPersonalIdError] = useState('')
    const [showPinModal, setShowPinModal] = useState(false)
    const bankId = useBankIdLogin()

    function handleBankIdSubmit(e: React.FormEvent) {
        e.preventDefault()
        if (personalId === '') {
            setPersonalIdError('login.errors.missingPersonalId')
            return
        }
        setPersonalIdError('')
        setShowPinModal(true)
    }

    // The pin is deliberately not passed anywhere, only for visual demo.
    function handlePinConfirm() {
        setShowPinModal(false)
        bankId.start(personalId)
    }
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
                    className="md:min-h-125 h-full bg-card/85! rounded-none md:rounded-card backdrop-blur-md border-white/70! dark:border-toggle!
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
                            onClick={() => setMode("email")}>
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
                <div className="text-small px-5 pb-5">
                    <form onSubmit={handleBankIdSubmit} className="space-y-4 ">
                        <div>
                            <p className="text-xsmall ml-1 text-muted font-bold">
                                {t("login.personalId")}
                            </p>
                            <input
                                type="text"
                                placeholder={t("login.personalIdPlaceholder")}
                                value={personalId}
                                onChange={(e) => setPersonalId(e.target.value)}
                                className="border border-border rounded-card px-3 py-2 w-full bg-card/70 transition duration-300 focus:outline-none focus:border-brand-text focus:ring-4 focus:ring-brand/15"
                            />
                        </div>
                        <div className="flex flex-col gap-3">
                            <button
                                type="submit"
                                disabled={bankId.status === 'pending'}
                                className="bg-brand hover:bg-brand/90 hover:shadow-lg hover:shadow-brand/25 transition duration-300 text-white rounded-card px-4 py-3 w-full disabled:opacity-50 mt-3"
                            >
                                {bankId.status === 'pending' ? t("login.bankIdWaiting") : t("login.bankIdSubmit")}
                            </button>
                             <button
                                type="submit"
                                disabled={bankId.status === 'pending'}
                                className="bg-background hover:bg-white text-brand border border-border rounded-card px-4 py-3 w-full disabled:opacity-50"
                            >
                                {bankId.status === 'pending' ? t("login.bankIdWaiting") : t("login.bankIdOtherDevice")}
                            </button>
                            {bankId.status === 'pending' && (
                                <button type="button" onClick={bankId.cancel} className="border border-border rounded-card px-4 py-2">
                                    {t("login.cancel")}
                                </button>
                            )}
                        </div>
                        <div className="flex justify-center text-sm">
                            {personalIdError && <p>{t(personalIdError)}</p>}
                            {bankId.errorMessage && <p>{t(bankId.errorMessage)}</p>}
                        </div>
                                        </form>
                </div>
            )}

            <p className="mx-3 mt-5 pt-5 text-small border-t-2 border-border">
                Vill du bli ny kund? Snart kommer du kunna registrera dig som ny kund hos oss.
            </p>
            </div>

            {showPinModal && (
                <BankIdPinModal
                    onConfirm={handlePinConfirm}
                    onCancel={() => setShowPinModal(false)}
                />
            )}
            {/* end BankID */}

                </Card>
            </div>
        </div>
    )
}