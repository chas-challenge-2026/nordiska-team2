import { Navigate, Outlet } from "react-router-dom";
import { useAuth } from "../../hooks/useAuth"
import { useTranslation } from "react-i18next"

export default function ProtectedRoute() {
    const auth = useAuth()
    const { t } = useTranslation()
 
    if (auth.isInitializing) {
        return <p>{t("common.loading")}</p>
    }
 
    if (auth.accessToken == null) {
        return <Navigate to="/login" />
    }
    return <Outlet />
}
 