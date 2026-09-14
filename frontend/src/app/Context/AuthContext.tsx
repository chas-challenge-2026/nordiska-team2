import { createContext, useState, useEffect } from 'react'
import { apiClient, setAuthToken, registerTokenChangeHandler } from '../../client'

type AuthContextType = {
    accessToken: string | null
    isInitializing: boolean
    setAccessToken: (token: string | null) => void
}

export const AuthContext = createContext<AuthContextType | null>(null)

export function AuthProvider({ children }: { children: React.ReactNode }) {
    const [accessToken, setAccessTokenState] = useState<string | null>(null)
    // Starts true: ProtectedRoute waits for this before deciding whether
    // to redirect to /login. Without it, accessToken's initial null
    // value causes every page reload to redirect BEFORE the silent
    // refresh below has a chance to complete -- even when it succeeds.
    // THIS HAS BEEN ACCIDENTALLY REMOVED FROM THIS FILE MULTIPLE TIMES.
    // If you're editing this file: keep this state, keep setAccessToken
    // as a wrapper (not the raw setter), and keep registerTokenChangeHandler.
    const [isInitializing, setIsInitializing] = useState(true)

    function setAccessToken(token: string | null) {
        setAccessTokenState(token)
        setAuthToken(token) // keeps apiClient's Authorization header in sync
    }

    useEffect(() => {
        // Lets client.ts's interceptor update this state when it
        // silently refreshes a token in the background.
        registerTokenChangeHandler(setAccessTokenState)

        async function restoreSession() {
            try {
                const response = await apiClient.post('/auth/refresh')
                setAccessToken(response.data.accessToken)
            } catch (error) {
                // no valid session, stay logged out.
            } finally {
                setIsInitializing(false)
            }
        }
        restoreSession()
    }, [])

    return (
        <AuthContext.Provider value={{ accessToken, isInitializing, setAccessToken }}>
            {children}
        </AuthContext.Provider>
    )
}