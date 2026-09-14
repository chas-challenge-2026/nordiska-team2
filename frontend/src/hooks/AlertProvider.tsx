import { useCallback, useState, type ReactNode } from "react";
import Alert from "../components/ui/Alert";
import { AlertContext, type AlertItem, type showAlertInput } from "./AlertContext";

export function AlertProvider({ children }: { children: ReactNode }) {
    const [alerts, setAlerts] = useState<AlertItem[]>([]);

    const dismissAlert = useCallback((id: string) => {
        setAlerts((current) => current.filter((alert) => alert.id !== id))
    }, [] )

    const showAlert = useCallback(
        ({ duration = 5000, ...toast }: showAlertInput) => {
            const id = crypto.randomUUID();
            setAlerts((current) => [...current, { ...toast, id }]);
            setTimeout(() => dismissAlert(id), duration)
        }, [dismissAlert]
    )

    return (
        <AlertContext.Provider value={{ showAlert }}>
            {children}

            <div className=" fixed bottom-4 right-4 z-50 
                            flex flex-col gap-2 ">
                {alerts.map((alert) => (
                    <Alert 
                    key={alert.id}
                    type={alert.type}
                    title={alert.title}
                    message={alert.message} />
                ))}
            </div>
        </AlertContext.Provider>
    )
}