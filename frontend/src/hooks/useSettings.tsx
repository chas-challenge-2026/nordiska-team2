import { useContext } from 'react'
import { SettingsContext } from '../app/Context/SettingsContext'

export function useSettings() {
    const context = useContext(SettingsContext)
    if (!context) {
        throw new Error('useSettings måste användas inuti SettingsProvider')
    }
    return context
}

