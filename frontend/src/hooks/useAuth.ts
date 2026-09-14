import { useContext } from "react"
import { AuthContext } from "../app/Context/AuthContext"

export function useAuth() {
    const auth = useContext(AuthContext)
    if (!auth) {
        throw new Error("useAuth has to be inside AuthProvider")
    }
    return auth
}