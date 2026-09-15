import { Navigate, Outlet } from "react-router-dom";
import { useAuth } from "../../hooks/useAuth"

export default function ProtectedRoute() {
    const auth = useAuth()
 
    if (auth.isInitializing) {
        return <p>Laddar...</p>
    }
 
    if (auth.accessToken == null) {
        return <Navigate to="/login" />
    }
    return <Outlet />
}
 