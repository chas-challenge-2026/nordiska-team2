import { Navigate, Outlet } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";

// Wraps any route that requires a logged-in user. Waits for the
// silent-refresh attempt (AuthContext's isInitializing) to finish
// before deciding anything -- otherwise a genuinely logged-in user
// would get bounced to /login on every reload, since their access
// token is gone from memory until that refresh completes.
export default function ProtectedRoute() {
  const { accessToken, isInitializing } = useAuth();

  if (isInitializing) {
    // TODO (frontend dev): replace with a real loading spinner/skeleton.
    return <p>Laddar...</p>;
  }

  if (!accessToken) {
    return <Navigate to="/login" replace />;
  }

  return <Outlet />;
}