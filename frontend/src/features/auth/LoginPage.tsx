import { useState } from "react";
import Card from "../../components/cards/Card"
import { apiClient } from "../../client"
import { Eye, EyeOff } from "lucide-react"
import { useAuth } from "./useAuth"


export default function LoginPage() {
    const [email, setEmail] = useState('')
    const [password, setPassword] = useState('')
    const [errorMessage, setErrorMessage] = useState('')
    const [isLoading, setIsLoading] = useState(false)
    const [showPassword, setShowPassword] = useState(false)
    const auth = useAuth()
    

       async function handleSubmit(e: React.FormEvent) {
                e.preventDefault()
                if (email === '' || password === '') {
                    setErrorMessage('Vänligen fyll i både e-post och lösenord')
                } else {
                    setIsLoading(true)
                    try {
                    const response = await apiClient.post('/auth/login', { email, password })
                    auth.setAccessToken(response.data.accessToken)
                    setIsLoading(false)
                } catch (error) {
                    setErrorMessage('Fel e-post eller lösenord, försök igen!')
                    
                    setIsLoading(false)
                }

            }

            }

    return ( 
        <div className="flex items-center justify-center min-h-screen">   
            <Card title="Logga in" className="w-full max-w-sm mx-4">
            <form onSubmit={handleSubmit} className="space-y-4">
                
                <input type="email" placeholder="E-post" value={email} onChange={(e) => setEmail(e.target.value)} className="border border-border rounded-default px-3 py-2 w-full"/>
                <div className="relative">
                <input type={showPassword ? "text" : "password"} placeholder="Lösenord" value={password} onChange={(e) => setPassword(e.target.value)} className="border border-border rounded-default px-3 py-2 w-full"/>
                <button type="button" onClick={() => setShowPassword(!showPassword)} className="absolute right-3 top-1/2 -translate-y-1/2">
                     {showPassword ? <Eye /> : <EyeOff />}
                </button>
                </div>
                <div className="flex justify-center">
                <button className="bg-brand hover:bg-brand/90 text-white rounded-default px-4 py-2">{isLoading ? 'Loggar in...' : 'Logga in'}</button>
                </div>
                <div className="flex justify-center text-sm">
                {errorMessage && <p>{errorMessage}</p>}
                </div>

                
            </form>
            </Card>

        </div>
    )
}

