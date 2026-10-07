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


export default function LoginPage() {
    const [email, setEmail] = useState('')
    const [password, setPassword] = useState('')
    const [errorMessage, setErrorMessage] = useState('')
    const [isLoading, setIsLoading] = useState(false)
    const [showPassword, setShowPassword] = useState(false)
    const [mode, setMode] = useState<"email" | "bankId">("email");

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
            setPersonalIdError('Vänligen fyll i personnummer')
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
                    setErrorMessage('Vänligen fyll i både e-post och lösenord')
                } else {
                    setIsLoading(true)
                    try {
                    const response = await apiClient.post('/auth/login', { email, password })
                    auth.setAccessToken(response.data.accessToken)
                    navigate("/dashboard")
                    setIsLoading(false)
                } catch {
                    setErrorMessage('Fel e-post eller lösenord, försök igen!')
                    setIsLoading(false)
                }

            }

            }

    return ( 
        <div className="relative isolate grid min-h-screen lg:grid-cols-2"> 
            <BlobBackground />

            {/* ========== Vänster sida ========== */}
            {/* Panelen "flyter" som ett rundat kort, så att det inte blir någon hård kant mot högersidan */}
            <div className="relative overflow-hidden hidden lg:m-4 lg:rounded-3xl lg:p-16 lg:flex lg:flex-col lg:gap-15
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
                        Morgondagens bank
                        <span className="block ml-auto">– för alla</span>
                    </h1>
                    <p className="text-small text-white opacity-60 italic">
                        Där finans­iering och teknik möts
                    </p>
                    <img src={heroImage} className="mt-10 mx-auto rounded-xl opacity-90
                                                    shadow-2xl shadow-black/30" />
                    <div className="flex gap-3 mt-2">
                                    {["Eget sparmål", 
                                    "Insättning och uttag", 
                                    "FAQ-assistent",
                                    "Skatterapporter"].map((feature) => (
                                        <div key={feature}
                                            className="flex items-center gap-2 text-xsmall text-white bg-white/20 rounded-full 
                                                        px-3 py-1 mt-3">
                                                <img src={checkIcon} className="size-4"/>
                                                <span>{feature}</span>
                                        </div>
                                    ))}
                    </div>
                </div>
                
            </div>

            {/* ========== Höger sida ========== */}
            <div className="m-auto w-3/5">   
                <Card 
                    headerContent={
                        <div className="mx-5 my-1">
                            <h2 className="text-title">Välkommen!</h2>
                            <p className="text-small text-white opacity-60">
                                Logga in för att se dina konton och sparmål.
                            </p>
                        </div>
                    } 
                    headerClassName="bg-linear-to-br from-brand to-[#0f3550] dark:from-[#16405e] dark:to-sidebar border-none"
                    className="min-h-125 bg-card/85! backdrop-blur-md border-white/70! dark:border-border!
                               shadow-2xl! shadow-brand/15! dark:shadow-black/40!">
      
            <div className="relative bg-border flex rounded-default border border-border overflow-hidden m-5">
                <div className={`absolute inset-y-0 w-1/2 bg-white border-2 border-border rounded-default
                    transition-transform duration-500 ease-in-out
                    ${mode === "email" ? "translate-x-0" : "translate-x-full"}`} />
                <button
                    type="button"
                    className={`relative z-10 flex-1 p-2 transition-colors text-small
                                ${mode === "email" ? "text-brand" : "text-muted bg-border"}`}
                    onClick={() => setMode("email")}>
                        E-post
                </button>
                <button
                    type="button"
                    className={`relative z-10 flex-1 p-2 transition-colors text-small
                                ${mode === "bankId" ? "text-brand" : "text-muted bg-border"}`}
                    onClick={() => setMode("bankId")}>
                        BankID
                </button>
            </div>

            {mode === "email" && (
                <div className="text-small px-5 pb-5">
                    <form onSubmit={handleSubmit}
                        className="flex flex-col gap-2 w-full">
                        <div>
                            <p className="text-xsmall ml-1 text-muted font-bold">E-post</p>
                            <input type="email" placeholder="E-post" value={email} onChange={(e) => setEmail(e.target.value)} className="border border-border rounded-default px-3 py-2 w-full bg-card/70 transition duration-300 focus:outline-none focus:border-brand-text focus:ring-4 focus:ring-brand/15"/>
                        </div>
                        
                        <div>
                            <p className="text-xsmall ml-1 text-muted font-bold">Lösenord</p>
                            <div className="relative">
                                <input type={showPassword ? "text" : "password"} placeholder="Lösenord" value={password} onChange={(e) => setPassword(e.target.value)} className="border border-border rounded-default px-3 py-2 w-full bg-card/70 transition duration-300 focus:outline-none focus:border-brand-text focus:ring-4 focus:ring-brand/15"/>
                                <button type="button" onClick={() => setShowPassword(!showPassword)} className="absolute right-3 top-1/2 -translate-y-1/2">
                                    {showPassword ? <Eye /> : <EyeOff />}
                                </button>
                            </div>
                        </div>

                        <div className="flex justify-center">
                        <button className="bg-brand hover:bg-brand/90 hover:shadow-lg hover:shadow-brand/25 transition duration-300 text-white font-bold rounded-default px-4 py-3 w-full mt-3">{isLoading ? 'Loggar in...' : 'Logga in'}</button>
                        </div>
                        <div className="flex justify-center text-sm">
                        {errorMessage && <p>{errorMessage}</p>}
                        </div>
                    
                    </form>
                </div>
            )}

            {mode === "bankId" && (
                <div className="text-small px-5 pb-5">
                    <form onSubmit={handleBankIdSubmit} className="space-y-4 ">
                        <div>
                            <p className="text-xsmall ml-1 text-muted font-bold">
                                Personnummer
                            </p>
                            <input
                                type="text"
                                placeholder="ÅÅÅÅMMDD-XXXX"
                                value={personalId}
                                onChange={(e) => setPersonalId(e.target.value)}
                                className="border border-border rounded-default px-3 py-2 w-full bg-card/70 transition duration-300 focus:outline-none focus:border-brand-text focus:ring-4 focus:ring-brand/15"
                            />
                        </div>
                        <div className="flex flex-col gap-3">
                            <button
                                type="submit"
                                disabled={bankId.status === 'pending'}
                                className="bg-brand hover:bg-brand/90 hover:shadow-lg hover:shadow-brand/25 transition duration-300 text-white rounded-default px-4 py-3 w-full disabled:opacity-50 mt-3"
                            >
                                {bankId.status === 'pending' ? 'Väntar på BankID...' : 'Logga in med BankID'}
                            </button>
                             <button
                                type="submit"
                                disabled={bankId.status === 'pending'}
                                className="bg-background hover:bg-white text-brand border border-border rounded-default px-4 py-3 w-full disabled:opacity-50"
                            >
                                {bankId.status === 'pending' ? 'Väntar på BankID...' : 'BankID på annan enhet'}
                            </button>
                            {bankId.status === 'pending' && (
                                <button type="button" onClick={bankId.cancel} className="border border-border rounded-default px-4 py-2">
                                    Avbryt
                                </button>
                            )}
                        </div>
                        <div className="flex justify-center text-sm">
                            {personalIdError && <p>{personalIdError}</p>}
                            {bankId.errorMessage && <p>{bankId.errorMessage}</p>}
                        </div>
                    </form>
                </div>
            )}

            <p className="mx-3 mt-5 pt-5 text-small border-t-2 border-border">
                Vill du bli ny kund? Snart kommer du kunna registrera dig som ny kund hos oss.
            </p>


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