import { createContext, useEffect, useState } from 'react'

export type Theme = 'light' | 'dark'

type SettingsContextType = {
    theme: Theme
    setTheme: (theme: Theme) => void
}

export const SettingsContext = createContext<SettingsContextType | null>(null)

function getInitialTheme(): Theme {
    try {
        const saved = localStorage.getItem('theme')
        if (saved === 'light' || saved === 'dark') return saved
    } catch {
        // localStorage kan vara blockerat, då kör vi på default
    }
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
}

export function SettingsProvider({ children }: { children: React.ReactNode }) {
    const [theme, setTheme] = useState<Theme>(getInitialTheme)

    
    useEffect(() => {
        document.documentElement.classList.toggle('dark', theme === 'dark')
        try {
            localStorage.setItem('theme', theme)
        } catch {
            
        }
    }, [theme])

    return (
        <SettingsContext.Provider value={{ theme, setTheme }}>
            {children}
        </SettingsContext.Provider>
    )
}  