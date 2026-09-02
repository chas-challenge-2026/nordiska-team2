import { useState } from "react";
import Card from "../../components/cards/Card"

export default function LoginPage() {
    const [email, setEmail] = useState('')
    const [password, setPassword] = useState('')
    const [errorMessage, setErrorMessage] = useState('')
    const [isLoading, setIsLoading] = useState(false)

       function handleSubmit(e: React.FormEvent) {
                e.preventDefault()
                if (email === '' || password === '') {
                    setErrorMessage('Vänligen fyll i både e-post och lösenord')
                } else {
                    setIsLoading(true)
                }

            }

    return ( 
        <div className="flex items-center justify-center min-h-screen">   
            <Card title="Logga in" className="w-full max-w-sm mx-4">
            <form onSubmit={handleSubmit} className="space-y-4">
                
                <input type="email" placeholder="E-post" value={email} onChange={(e) => setEmail(e.target.value)} className="border border-border rounded-default px-3 py-2 w-full"/>
                <input type="password" placeholder="Lösenord" value={password} onChange={(e) => setPassword(e.target.value)} className="border border-border rounded-default px-3 py-2 w-full"/>
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

