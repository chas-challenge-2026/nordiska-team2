import { Navigate, Outlet } from "react-router-dom"
import { useAuth } from "./useAuth"

export default function ProtectedRoute() {
    const auth = useAuth()

    if (auth.accessToken == null) {
        return <Navigate to="/login" />
    }
}