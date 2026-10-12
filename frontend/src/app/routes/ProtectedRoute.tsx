import { Navigate, Outlet } from "react-router-dom";
import { useAuth } from "../../hooks/useAuth"
import { AssistantWidget } from "../../features/assistant";
import { refreshAccessToken } from "../../client";   

export default function ProtectedRoute() {
    const auth = useAuth()
 
    if (auth.isInitializing) {
        return <p>Laddar...</p>
    }
 
    if (auth.accessToken == null) {
        return <Navigate to="/login" />
    }
    return (
  <>
    <Outlet />
    {/* AI assistant independent feature.
        To remove: delete this line and src/features/assistant/. */}
    <AssistantWidget accessToken={auth.accessToken} refreshToken={refreshAccessToken} />
  </>
);
}