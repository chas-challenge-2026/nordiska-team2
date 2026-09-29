import { useState } from "react";
import Card from "../../components/cards/Card"
import { apiClient } from "../../client"
import { Eye, EyeOff } from "lucide-react"
import { useAuth } from "../../hooks/useAuth"
import { useNavigate } from "react-router-dom"
import { useBankIdLogin } from "../../hooks/useBankIdLogin"
import BankIdPinModal from "../../components/DemoBankIdPinModal"


export default function LoginPage() {
    const [email, setEmail] = useState('')
    const [password, setPassword] = useState('')
    const [errorMessage, setErrorMessage] = useState('')
    const [isLoading, setIsLoading] = useState(false)
    const [showPassword, setShowPassword] = useState(false)
    const auth = useAuth()
    const navigate = useNavigate()

    // --- BankID (new) ---
    const [personalId, setPersonalId] = useState('')
    const [showPinModal, setShowPinModal] = useState(false)
    const bankId = useBankIdLogin()

    function handleBankIdSubmit(e: React.FormEvent) {
        e.preventDefault()
        if (personalId === '') {
            setErrorMessage('Vänligen fyll i personnummer')
            return
        }
        setErrorMessage('')
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
                } catch (error) {
                    setErrorMessage('Fel e-post eller lösenord, försök igen!')
                    
                    setIsLoading(false)
                }

            }

            }

    return ( 
        <div className="flex items-center justify-center min-h-screen">   
            {/* Widened from max-w-sm to fit two columns side by side. */}
            <Card title="Logga in" className="w-full max-w-2xl mx-4">

            {/* Stacks on small screens, two columns from md up. */}
            <div className="grid grid-cols-1 md:grid-cols-[1fr_auto_1fr] gap-4 md:gap-6">

            <div>
            <form onSubmit={handleSubmit} className="space-y-4">
                
                <input type="email" placeholder="E-post" value={email} onChange={(e) => setEmail(e.target.value)} className="border border-border rounded-default px-3 py-2 w-full"/>
                <div className="relative">
                <input type={showPassword ? "text" : "password"} placeholder="Lösenord" value={password} onChange={(e) => setPassword(e.target.value)} className="border border-border rounded-default px-3 py-2 w-full"/>
                <button type="button" onClick={() => setShowPassword(!showPassword)} className="absolute right-3 top-1/2 -translate-y-1/2">
                     {showPassword ? <Eye /> : <EyeOff />}
                </button>
                </div>

                {/* <input type="password" placeholder="Lösenord" value={password} onChange={(e) => setPassword(e.target.value)} className="border border-border rounded-default px-3 py-2 w-full"/> */}

                <div className="flex justify-center">
                <button className="bg-brand hover:bg-brand/90 text-white rounded-default px-4 py-2">{isLoading ? 'Loggar in...' : 'Logga in'}</button>
                </div>
                <div className="flex justify-center text-sm">
                {errorMessage && <p>{errorMessage}</p>}
                </div>

                
            </form>
            </div>

            {/* BankID (new) */}

            {/* Divider: horizontal line when stacked, vertical when side by side. */}
            <div className="flex md:flex-col items-center gap-3">
                <div className="h-px w-full md:h-full md:w-px bg-border" />
                <span className="text-muted text-small">eller</span>
                <div className="h-px w-full md:h-full md:w-px bg-border" />
            </div>

            <div>
            <form onSubmit={handleBankIdSubmit} className="space-y-4">
                <input
                    type="text"
                    placeholder="Personnummer (ÅÅÅÅMMDD-XXXX)"
                    value={personalId}
                    onChange={(e) => setPersonalId(e.target.value)}
                    className="border border-border rounded-default px-3 py-2 w-full"
                />
                <div className="flex flex-col gap-2">
                    <button
                        type="submit"
                        disabled={bankId.status === 'pending'}
                        className="bg-brand hover:bg-brand/90 text-white rounded-default px-4 py-2 disabled:opacity-50"
                    >
                        {bankId.status === 'pending' ? 'Väntar på BankID...' : 'Logga in med BankID'}
                    </button>
                    {bankId.status === 'pending' && (
                        <button type="button" onClick={bankId.cancel} className="border border-border rounded-default px-4 py-2">
                            Avbryt
                        </button>
                    )}
                </div>
                <div className="flex justify-center text-sm">
                    {bankId.errorMessage && <p>{bankId.errorMessage}</p>}
                </div>
            </form>
            </div>

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
    )
}