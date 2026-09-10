import { createContext, useState, useEffect } from 'react'
import { apiClient } from '../../client'

type AuthContextType = {
    accessToken: string | null 
    setAccessToken: (token: string | null) => void
}

export const AuthContext = createContext<AuthContextType | null>(null)

export function AuthProvider({ children } : { children: React.ReactNode }) {
    const [accessToken, setAccessToken] = useState<string | null>(null)

    useEffect(() => { 
    async function restoreSession() {
        try {
            const response = await apiClient.post('/auth/refresh')
            setAccessToken(response.data.accessToken)
        } catch (error) {
            //no valid session, stay logged out.
        }
    }
    restoreSession()
    }, [])

    return (
        <AuthContext.Provider value={{ accessToken, setAccessToken }}>
            {children}
        </AuthContext.Provider>
    )
}